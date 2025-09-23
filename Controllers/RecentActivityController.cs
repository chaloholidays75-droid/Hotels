using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/recent")]
    public class RecentActivitiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecentActivitiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/recent/list
        [HttpGet("list")]
        public async Task<IActionResult> GetRecentListActivities()
        {
            var activities = await _context.RecentActivities
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return Ok(activities);
        }

        // GET: api/recent?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int page = 1, int pageSize = 20)
        {
            var activities = await _context.RecentActivities
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(activities);
        }

        // POST: api/recent/fix-old-activities
        [HttpPost("fix-old-activities")]
        public async Task<IActionResult> FixOldActivities()
        {
            var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.Email);

            // Fix Agencies
            var agencies = await _context.Agencies.ToListAsync();
            foreach (var agency in agencies)
            {
                var activities = _context.RecentActivities
                    .Where(r => r.Entity == "Agencies" && r.EntityId == agency.Id && r.UserId == 0);

                foreach (var act in activities)
                {
                    if (act.Action == "CREATE" && agency.CreatedById.HasValue)
                    {
                        act.UserId = agency.CreatedById.Value;
                        act.UserName = users.ContainsKey(agency.CreatedById.Value)
                            ? users[agency.CreatedById.Value]
                            : "Legacy User";
                    }
                    else if ((act.Action == "UPDATE" || act.Action == "DELETE") && agency.UpdatedById.HasValue)
                    {
                        act.UserId = agency.UpdatedById.Value;
                        act.UserName = users.ContainsKey(agency.UpdatedById.Value)
                            ? users[agency.UpdatedById.Value]
                            : "Legacy User";
                    }
                }
            }

            // Fix HotelInfo
            var hotels = await _context.HotelInfo.ToListAsync();
            foreach (var hotel in hotels)
            {
                var activities = _context.RecentActivities
                    .Where(r => r.Entity == "HotelInfo" && r.EntityId == hotel.Id && r.UserId == 0);

                foreach (var act in activities)
                {
                    if (act.Action == "CREATE" && hotel.CreatedById.HasValue)
                    {
                        int createdById = hotel.CreatedById.Value;
                        act.UserId = createdById;
                        act.UserName = users.ContainsKey(createdById) ? users[createdById] : "Legacy User";
                    }
                    else if ((act.Action == "UPDATE" || act.Action == "DELETE") && hotel.UpdatedById.HasValue)
                    {
                        int updatedById = hotel.UpdatedById.Value;
                        act.UserId = updatedById;
                        act.UserName = users.ContainsKey(updatedById) ? users[updatedById] : "Legacy User";
                    }

                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Old RecentActivity records updated successfully!" });
        }
        [HttpPost("backfill")]
public async Task<IActionResult> BackfillActivities()
{
    // Load all users to map UserId -> UserName
    var users = await _context.Users
        .Where(u => u.IsActive)
        .ToDictionaryAsync(u => u.Id, u => u.Email ?? $"{u.FirstName} {u.LastName}");

    var activities = new List<RecentActivity>();

    // --- Agencies ---
    var agencies = await _context.Agencies.ToListAsync();
    foreach (var agency in agencies)
    {
        // CREATE action
        var createAct = new RecentActivity
        {
            Entity = "Agencies",
            EntityId = agency.Id,
            Action = "CREATE",
            Description = $"{agency.AgencyName} existed before logging",
            Timestamp = agency.CreatedAt,
            UserId = agency.CreatedById ?? 0,
            UserName = agency.CreatedById.HasValue && users.ContainsKey(agency.CreatedById.Value)
                        ? users[agency.CreatedById.Value]
                        : "System"
        };
        activities.Add(createAct);

        // UPDATE action if UpdatedAt is different from CreatedAt
        if (agency.UpdatedAt > agency.CreatedAt)
        {
            var updateAct = new RecentActivity
            {
                Entity = "Agencies",
                EntityId = agency.Id,
                Action = "UPDATE",
                Description = $"{agency.AgencyName} was updated",
                Timestamp = agency.UpdatedAt,
                UserId = agency.UpdatedById?? 0,
                UserName = agency.UpdatedById.HasValue && users.ContainsKey(agency.UpdatedById.Value)
                            ? users[agency.UpdatedById.Value]
                            : "System"
            };
            activities.Add(updateAct);
        }
    }

    // --- Hotels ---
    var hotels = await _context.HotelInfo.ToListAsync();
    foreach (var hotel in hotels)
    {
        // CREATE action
        var createAct = new RecentActivity
        {
            Entity = "HotelInfo",
            EntityId = hotel.Id,
            Action = "CREATE",
            Description = $"{hotel.HotelName} existed before logging",
            Timestamp = hotel.CreatedAt,
            UserId = hotel.CreatedById ?? 0,
            UserName = hotel.CreatedById.HasValue && users.ContainsKey(hotel.CreatedById.Value)
                        ? users[hotel.CreatedById.Value]
                        : "System"
        };
        activities.Add(createAct);

        // UPDATE action if UpdatedAt is different from CreatedAt
        if (hotel.UpdatedAt > hotel.CreatedAt)
        {
            var updateAct = new RecentActivity
            {
                Entity = "HotelInfo",
                EntityId = hotel.Id,
                Action = "UPDATE",
                Description = $"{hotel.HotelName} was updated",
                Timestamp = hotel.UpdatedAt ?? DateTime.UtcNow,
                UserId = hotel.UpdatedById ?? 0,
                UserName = hotel.UpdatedById.HasValue && users.ContainsKey(hotel.UpdatedById.Value)
                            ? users[hotel.UpdatedById.Value]
                            : "System"
            };
            activities.Add(updateAct);
        }
    }

    // Add all backfill activities at once
    _context.RecentActivities.AddRange(activities);
    await _context.SaveChangesAsync();

    return Ok(new { Message = $"Backfilled {activities.Count} activity records successfully." });
}

    }
}
