using Microsoft.AspNetCore.Mvc.Filters;
using HotelAPI.Data;
using HotelAPI.Models;

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

            if (resultContext.Exception == null) // only log successful actions
            {
                var httpMethod = resultContext.HttpContext.Request.Method;
                string actionType = httpMethod switch
                {
                    "POST" => "Add",
                    "PUT" => "Edit",
                    "PATCH" => "Edit",
                    "DELETE" => "Delete",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(actionType))
                {
                    var entityName = resultContext.Controller.GetType().Name.Replace("Controller", "");
                    var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                    int entityId = 0;

                    // Try to extract entity Id from body or route
                    if (context.ActionArguments.Values.FirstOrDefault() is HotelAPI.Models.Agency agency)
                        entityId = agency.Id;
                    if (context.ActionArguments.Values.FirstOrDefault() is HotelAPI.Models.HotelInfo hotel)
                        entityId = hotel.Id;
                    if (context.ActionArguments.ContainsKey("id"))
                        entityId = (int)context.ActionArguments["id"];

                    var log = new RecentActivity
                    {
                        Username = username,
                        ActionType = actionType,
                        Entity = entityName,
                        EntityId = entityId,
                        Description = $"{actionType} performed on {entityName} with ID {entityId}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.RecentActivities.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
