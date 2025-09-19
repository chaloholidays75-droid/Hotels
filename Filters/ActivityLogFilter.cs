using Microsoft.AspNetCore.Mvc.Filters;
using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System;

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

            if (resultContext.Exception != null) return; // Only log successful actions

            var httpMethod = resultContext.HttpContext.Request.Method;
            string actionType = httpMethod switch
            {
                "POST" => "Created",
                "PUT" => "Updated",
                "PATCH" => "Updated",
                "DELETE" => "Deleted",
                _ => ""
            };

            if (string.IsNullOrEmpty(actionType)) return;

            var username = _httpContextAccessor.HttpContext?.User?.FindFirst("FullName")?.Value ?? "Unknown user";

            int entityId = 0;
            string entityType = "";
            string entityName = "";

            // Detect entity from action arguments
            var firstArg = context.ActionArguments.Values.FirstOrDefault();
            if (firstArg is Agency agency)
            {
                entityId = agency.Id;
                entityType = "Agency";
                entityName = agency.AgencyName;
            }
            else if (firstArg is HotelInfo hotel)
            {
                entityId = hotel.Id;
                entityType = "Hotel";
                entityName = hotel.HotelName;
            }
            else if (context.ActionArguments.ContainsKey("id"))
            {
                entityId = (int)context.ActionArguments["id"];
                entityType = context.Controller.GetType().Name.Replace("Controller", "");
                entityName = ""; // optional, could query DB if needed
            }

            var log = new RecentActivity
            {
                Username = username,
                ActionType = actionType,
                Entity = entityType,
                EntityId = entityId,
                Description = $"{username} {actionType.ToLower()} {entityType} \"{entityName}\"",
                CreatedAt = DateTime.UtcNow
            };

            _context.RecentActivities.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
