using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;

namespace HotelAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecentActivityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecentActivityController(AppDbContext context)
        {
            _context = context;
        }

        // Full recent activity page (with optional count)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int count = 50)
        {
            var activities = await _context.RecentActivities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
            return Ok(activities);
        }

        // Mini dashboard version (last 5 actions)
        [HttpGet("mini")]
        public async Task<IActionResult> GetMini()
        {
            var activities = await _context.RecentActivities
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();
            return Ok(activities);
        }
    }
}
