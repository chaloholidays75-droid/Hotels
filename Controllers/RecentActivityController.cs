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
            // Clear old entries first (optional)
            var existing = _context.RecentActivities;
            _context.RecentActivities.RemoveRange(existing);
            await _context.SaveChangesAsync();

            // Load users dictionary
            var users = await _context.Users
                .Where(u => u.IsActive)
                .ToDictionaryAsync(u => u.Id, u => u.Email); // or u.UserName if you prefer

            // Backfill Agencies
            var agencies = await _context.Agencies.ToListAsync();
            foreach (var agency in agencies)
            {
                int? userId = agency.CreatedById ?? agency.UpdatedById;
                string userName = userId.HasValue && users.ContainsKey(userId.Value)
                    ? users[userId.Value]
                    : "System";

                var action = agency.CreatedAt <= agency.UpdatedAt ? "CREATE" : "UPDATE";

                var activity = new RecentActivity
                {
                    UserId = userId ?? 0,
                    UserName = userName,
                    Action = action,
                    Entity = "Agencies",
                    EntityId = agency.Id,
                    Description = $"{agency.AgencyName} {(action.ToLower())}d",
                    Timestamp = agency.UpdatedAt
                };

                _context.RecentActivities.Add(activity);
            }

            // Backfill HotelInfo
            var hotels = await _context.HotelInfo.ToListAsync();
            foreach (var hotel in hotels)
            {
                int? userId = hotel.CreatedById ?? hotel.UpdatedById;
                string userName = userId.HasValue && users.ContainsKey(userId.Value)
                    ? users[userId.Value]
                    : "System";

                var action = hotel.CreatedAt <= (hotel.UpdatedAt ?? hotel.CreatedAt) ? "INSERT" : "UPDATE";

                var activity = new RecentActivity
                {
                    UserId = userId ?? 0,
                    UserName = userName,
                    Action = action,
                    Entity = "HotelInfo",
                    EntityId = hotel.Id,
                    Description = $"{hotel.HotelName} {(action.ToLower())}d",
                    Timestamp = hotel.UpdatedAt ?? hotel.CreatedAt
                };

                _context.RecentActivities.Add(activity);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Backfill completed successfully." });
        }
    }
}
