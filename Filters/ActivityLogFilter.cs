using Microsoft.AspNetCore.Mvc.Filters;
using HotelAPI.Data;
using HotelAPI.Models;
using System.Security.Claims;

namespace HotelAPI.Filters
{
    public class ActivityLogFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogFilter(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (resultContext.Exception != null) return; // only log successful actions

            var httpMethod = resultContext.HttpContext.Request.Method;
            string actionType = httpMethod switch
            {
                "POST" => "Created",
                "PUT" => "Edited",
                "PATCH" => "Edited",
                "DELETE" => "Deleted",
                _ => ""
            };

            if (string.IsNullOrEmpty(actionType)) return;

            // Get entity name from controller
            var entityName = resultContext.Controller.GetType().Name.Replace("Controller", "");

            // Get user info from JWT claims
            var userClaims = _httpContextAccessor.HttpContext?.User;
            string username = userClaims?.FindFirst("FullName")?.Value
                              ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value
                              ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value
                              ?? "Unknown user";

            string role = userClaims?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown role";

            // Extract entity info from action arguments
            int entityId = 0;
            string entityLabel = "";

            foreach (var arg in context.ActionArguments.Values)
            {
                if (arg == null) continue;

                var argType = arg.GetType();
                var idProp = argType.GetProperty("Id");
                if (idProp != null) entityId = (int)(idProp.GetValue(arg) ?? 0);

                var nameProp = argType.GetProperty("AgencyName") ??
                               argType.GetProperty("HotelName") ??
                               argType.GetProperty("Name") ??
                               argType.GetProperty("Title");
                if (nameProp != null) entityLabel = nameProp.GetValue(arg)?.ToString() ?? "";
            }

            // Create the log object
            var log = new RecentActivity
            {
                Username = username,
                Role = role,
                ActionType = actionType.ToLower(),
                Entity = entityName.ToLower(),
                Description = entityLabel,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            };

            // Log to console
            Console.WriteLine($"[{log.CreatedAt}] {log.Username}  {log.ActionType} {log.Entity} -> {log.Description}");

            // Save to database
            _context.RecentActivities.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
