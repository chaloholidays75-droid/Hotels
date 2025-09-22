using Microsoft.AspNetCore.Mvc.Filters;
using HotelAPI.Data;
using HotelAPI.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelAPI.Filters
{
    public class ActivityLogFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ActivityLogFilter> _logger;

        public ActivityLogFilter(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<ActivityLogFilter> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (resultContext.Exception != null)
                return; // skip logging on exception

            var httpMethod = resultContext.HttpContext.Request.Method;
            string actionType = httpMethod switch
            {
                "POST" => "Created",
                "PUT" => "Edited",
                "PATCH" => "Edited",
                "DELETE" => "Deleted",
                _ => ""
            };

            if (string.IsNullOrEmpty(actionType))
                return;

            var entityName = resultContext.Controller.GetType().Name.Replace("Controller", "");

            // --- Log request info for debugging ---
            _logger.LogInformation("Request to {path} method {method}", resultContext.HttpContext.Request.Path, httpMethod);
            var authHeader = resultContext.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            _logger.LogInformation("Authorization header: {auth}", authHeader ?? "None");

            var userClaims = _httpContextAccessor.HttpContext?.User;

            if (userClaims?.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("User is authenticated. Claims:");
                foreach (var c in userClaims.Claims)
                {
                    _logger.LogInformation("  {type} = {value}", c.Type, c.Value);
                }
            }
            else
            {
                _logger.LogInformation("User is NOT authenticated (no claims).");
            }

            // --- Determine username ---
            string username = userClaims?.FindFirst("FullName")?.Value
                              ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value
                              ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value;

            // --- Fallback to DB ---
            if (string.IsNullOrEmpty(username) && userClaims != null)
            {
                var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                        username = $"{user.FirstName} {user.LastName}";
                }
            }

            // --- Final fallback ---
            if (string.IsNullOrEmpty(username))
                username = "System";

            // --- Extract entity info ---
            int entityId = 0;
            string entityLabel = "";
            foreach (var arg in context.ActionArguments.Values)
            {
                switch (arg)
                {
                    case HotelAPI.Models.Agency agency:
                        entityId = agency.Id;
                        entityLabel = agency.AgencyName;
                        break;
                    case HotelAPI.Models.HotelInfo hotel:
                        entityId = hotel.Id;
                        entityLabel = hotel.HotelName;
                        break;
                    case int id:
                        entityId = id;
                        break;
                }
            }

            // --- Create log entry ---
            var log = new RecentActivity
            {
                Username = username,
                ActionType = actionType,
                Entity = entityName,
                EntityId = entityId,
                Description = $"{username} {actionType.ToLower()} {entityName} \"{entityLabel}\"",
                CreatedAt = DateTime.UtcNow
            };

            _context.RecentActivities.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
