using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/recent-activities")]
    [Authorize] // Optional: require authentication if using JWT
    public class RecentActivitiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecentActivitiesController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ 1️⃣ Get all recent activities (with pagination)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var activities = await _context.RecentActivities
                .OrderByDescending(a => a.Timestamp)

                .ToListAsync();

            return Ok(activities);
        }

        // ✅ 2️⃣ Get latest 30 activities (for dashboard)
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            var result = await _context.RecentActivities
                .OrderByDescending(r => r.Timestamp)
                .Take(30)
                .Select(r => new
                {
                    r.Id,
                    r.UserName,
                    r.ActionType,
                    r.TableName,
                    r.RecordId,
                    r.Description,
                    r.Timestamp,
                    TimeAgo = GetTimeAgo(r.Timestamp)
                })
                .ToListAsync();

            return Ok(result);
        }

        // ✅ 3️⃣ Log a new activity manually
        [HttpPost("log")]
        public async Task<IActionResult> LogActivity([FromBody] RecentActivity request)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                string userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

                var activity = new RecentActivity
                {
                   
                    UserName = userName,
                    ActionType = request.ActionType,
                    TableName = request.TableName,
                    RecordId = request.RecordId,
                    Description = request.Description,
                    Timestamp = DateTime.UtcNow,
                    ChangedData = request.ChangedData
                };

                _context.RecentActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Activity logged successfully!", activity });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error logging activity", error = ex.Message });
            }
        }

        // ✅ 4️⃣ Optional: Get activities by table or user
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(string? tableName = null, string? user = null)
        {
            var query = _context.RecentActivities.AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(a => a.TableName == tableName);

            if (!string.IsNullOrEmpty(user))
                query = query.Where(a => a.UserName == user);

            var result = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            return Ok(result);
        }

        // ✅ 5️⃣ Helper: Convert to human-readable "time ago"
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
