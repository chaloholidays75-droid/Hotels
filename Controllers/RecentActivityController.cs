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
// POST: api/recent/backfill
[HttpPost("backfill")]
public async Task<IActionResult> BackfillOldActivities()
{
    // Fetch all users to map Id -> Email/UserName
    var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.Email);

    // Backfill Agencies
    var agencies = await _context.Agencies.ToListAsync();
    foreach (var agency in agencies)
    {
        // Check if there is already an activity for this entity to avoid duplicates
        var exists = await _context.RecentActivities
            .AnyAsync(a => a.Entity == "Agencies" && a.EntityId == agency.Id);
        if (!exists)
        {
            _context.RecentActivities.Add(new RecentActivity
            {
                UserId = agency.CreatedById ?? 0,
                UserName = agency.CreatedById.HasValue && users.ContainsKey(agency.CreatedById.Value)
                    ? users[agency.CreatedById.Value]
                    : "Legacy User",
                Action = "CREATE",
                Entity = "Agencies",
                EntityId = agency.Id,
                Description = $"{agency.AgencyName} existed before logging",
                Timestamp = agency.CreatedAt
            });
        }
    }

    // Backfill HotelInfo
    var hotels = await _context.HotelInfo.ToListAsync();
    foreach (var hotel in hotels)
    {
        var exists = await _context.RecentActivities
            .AnyAsync(a => a.Entity == "HotelInfo" && a.EntityId == hotel.Id);
        if (!exists)
        {
            _context.RecentActivities.Add(new RecentActivity
            {
                UserId = hotel.CreatedById ?? 0,
                UserName = hotel.CreatedById.HasValue && users.ContainsKey(hotel.CreatedById.Value)
                    ? users[hotel.CreatedById.Value]
                    : "Legacy User",
                Action = "CREATE",
                Entity = "HotelInfo",
                EntityId = hotel.Id,
                Description = $"{hotel.HotelName} existed before logging",
                Timestamp = hotel.CreatedAt
            });
        }
    }

    await _context.SaveChangesAsync();

    return Ok(new { message = "Backfill completed successfully!" });
}


    }
}
