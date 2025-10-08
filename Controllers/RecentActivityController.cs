using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

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

        // POST: api/recent/log
        // [HttpPost("log")]
        // public async Task<IActionResult> LogActivity([FromBody] RecentActivity activity)
        // {
        //     // Get UserId and UserName from JWT
        //     int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        //     string userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

        //     activity.UserId = userId;
        //     activity.UserName = userName;
        //     activity.Timestamp = System.DateTime.UtcNow;

        //     _context.RecentActivities.Add(activity);
        //     await _context.SaveChangesAsync();

        //     return Ok(new { message = "Activity logged successfully!" });
        // }

        // Helper method for automatic logging
        [HttpPost("log")]
        public async Task LogActionAsync(string entity, int entityId, string action, string description)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            string userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var activity = new RecentActivity
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Description = description,
                Timestamp = System.DateTime.UtcNow
            };

            _context.RecentActivities.Add(activity);
            await _context.SaveChangesAsync();
        }
               // ========== 8. RECENT ACTIVITIES ==========

        [HttpGet("recent-activities")]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                // 1️⃣ Fetch from database first
                var activities = await _context.RecentActivities
                    .OrderByDescending(r => r.Timestamp)
                    .Take(20)
                    .ToListAsync(); // materialize query

                // 2️⃣ Project in memory, including TimeAgo
                var result = activities.Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.UserName,
                    r.Action,
                    r.Entity,
                    r.EntityId,
                    r.Description,
                    r.Timestamp,
                    TimeAgo = GetTimeAgo(r.Timestamp) // safe now
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error fetching recent activities");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ===== Helper: Human-readable time ago =====
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds} seconds ago";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours} hours ago";
            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days} days ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

    
    }
}
