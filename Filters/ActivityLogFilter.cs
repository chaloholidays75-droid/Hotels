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

                    // Get username from JWT
                    var userClaims = _httpContextAccessor.HttpContext?.User;
                    string username = userClaims?.FindFirst("FullName")?.Value   // custom FullName claim
                                      ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value // Identity.Name fallback
                                      ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value // Email fallback
                                      ?? "Unknown user";

                    int entityId = 0;
                    string entityLabel = "";

                    // Extract entity info from action arguments
                    foreach (var arg in context.ActionArguments.Values)
                    {
                        if (arg is HotelAPI.Models.Agency agency)
                        {
                            entityId = agency.Id;
                            entityLabel = agency.AgencyName;
                        }
                        else if (arg is HotelAPI.Models.HotelInfo hotel)
                        {
                            entityId = hotel.Id;
                            entityLabel = hotel.HotelName;
                        }
                        else if (arg is int id)
                        {
                            entityId = id;
                        }
                    }

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
    }
}
