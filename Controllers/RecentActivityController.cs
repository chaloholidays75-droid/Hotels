// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using HotelAPI.Data;
// using HotelAPI.Models;
// using Microsoft.AspNetCore.Authorization;

// namespace HotelAPI.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class RecentActivityController : ControllerBase
//     {
//         private readonly AppDbContext _context;

//         public RecentActivityController(AppDbContext context)
//         {
//             _context = context;
//         }

//         // Full recent activity page (with optional count)
//         [HttpGet]
//         public async Task<IActionResult> GetAll([FromQuery] int count = 50)
//         {
//             var activities = await _context.RecentActivities
//                 .OrderByDescending(a => a.CreatedAt)
//                 .Take(count)
//                 .ToListAsync();
//             return Ok(activities);
//         }

//         // Mini dashboard version (last 5 actions)
//         [HttpGet("mini")]
//         public async Task<IActionResult> GetMini()
//         {
//             var activities = await _context.RecentActivities
//                 .OrderByDescending(a => a.CreatedAt)
//                 .Take(5)
//                 .ToListAsync();
//             return Ok(activities);
//         }

//         // Create a new recent activity
//         [HttpPost]
//         [Authorize] // Require authentication
//         public async Task<IActionResult> Create([FromBody] RecentActivity activity)
//         {
//             if (activity == null)
//                 return BadRequest("Invalid activity");

//             // Capture username from logged-in user
//             activity.Username = User.Identity?.Name ?? "Unknown user";
//             activity.CreatedAt = DateTime.UtcNow;

//             // Optional: Build description automatically if not provided
//             if (string.IsNullOrEmpty(activity.Description))
//             {
//                 activity.Description = $"{activity.ActionType} {activity.Entity} \"{activity.EntityId}\"";
//             }

//             _context.RecentActivities.Add(activity);
//             await _context.SaveChangesAsync();

//             return CreatedAtAction(nameof(GetAll), new { id = activity.Id }, activity);
//         }
//         [HttpGet("paged")]
//         public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//         {
//             var activities = await _context.RecentActivities
//                 .OrderByDescending(a => a.CreatedAt)
//                 .Skip((page - 1) * pageSize)
//                 .Take(pageSize)
//                 .ToListAsync();
//             return Ok(activities);
//         }

//     }
// }


using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/recent")]  // <-- this is the endpoint
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
    }
}

