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

            if (resultContext.Exception == null) // only log successful actions
            {
                var httpMethod = resultContext.HttpContext.Request.Method;
                string actionType = httpMethod switch
                {
                    "POST" => "Created",
                    "PUT" => "Edited",
                    "PATCH" => "Edited",
                    "DELETE" => "Deleted",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(actionType))
                {
                    var entityName = resultContext.Controller.GetType().Name.Replace("Controller", "");

                    // Get username and role from JWT claims
                    var userClaims = _httpContextAccessor.HttpContext?.User;
                    string username = userClaims?.FindFirst("FullName")?.Value
                                      ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value
                                      ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value
                                      ?? "Unknown user";

                    string role = userClaims?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown role";

                    int entityId = 0;
                    string entityLabel = "";

                    // Try to extract entity info from action arguments
                    foreach (var arg in context.ActionArguments.Values)
                    {
                        var argType = arg?.GetType();
                        if (arg == null) continue;

                        // Check for Id property
                        var idProp = argType.GetProperty("Id");
                        if (idProp != null)
                        {
                            entityId = (int)(idProp.GetValue(arg) ?? 0);
                        }

                        // Check for Name/Title/Label property
                        var nameProp = argType.GetProperty("AgencyName") ?? 
                                       argType.GetProperty("HotelName") ??
                                       argType.GetProperty("Name") ??
                                       argType.GetProperty("Title");
                        if (nameProp != null)
                        {
                            entityLabel = nameProp.GetValue(arg)?.ToString() ?? "";
                        }
                    }

                    // Create activity log
                    var log = new RecentActivity
                    {
                        Username = username,
                        ActionType = actionType,
                        Entity = entityName,
                        EntityId = entityId,
                        Description = $"{username} ({role}) {actionType.ToLower()} {entityName} \"{entityLabel}\"",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.RecentActivities.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
