using Microsoft.AspNetCore.Mvc.Filters;
using HotelAPI.Data;
using HotelAPI.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

                    // Get username from JWT claims
                    var userClaims = _httpContextAccessor.HttpContext?.User;
                    string username = userClaims?.FindFirst("FullName")?.Value
                                      ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value
                                      ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value
                                      ?? null; // temporarily null

                    // If JWT did not have username, fallback to DB lookup
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

                    // Final fallback
                    if (string.IsNullOrEmpty(username))
                        username = "System";

                    int entityId = 0;
                    string entityLabel = "";

                    // Extract entity info from action arguments
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
