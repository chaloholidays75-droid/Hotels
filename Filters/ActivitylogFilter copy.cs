// using Microsoft.AspNetCore.Mvc.Filters;
// using HotelAPI.Data;
// using HotelAPI.Models;
// using System.Security.Claims;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;

// namespace HotelAPI.Filters
// {
//     public class ActivityLogFilter : IAsyncActionFilter
//     {
//         private readonly AppDbContext _context;
//         private readonly IHttpContextAccessor _httpContextAccessor;
//         private readonly ILogger<ActivityLogFilter> _logger;

//         public ActivityLogFilter(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<ActivityLogFilter> logger)
//         {
//             _context = context;
//             _httpContextAccessor = httpContextAccessor;
//             _logger = logger;
//         }

//         public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//         {
//             var resultContext = await next();

//             if (resultContext.Exception != null)
//                 return; // skip logging on exception

//             var httpMethod = resultContext.HttpContext.Request.Method;
//             string actionType = httpMethod switch
//             {
//                 "POST" => "Created",
//                 "PUT" => "Edited",
//                 "PATCH" => "Edited",
//                 "DELETE" => "Deleted",
//                 _ => ""
//             };

//             if (string.IsNullOrEmpty(actionType))
//                 return;

//             var entityName = resultContext.Controller.GetType().Name.Replace("Controller", "");

//             // --- Log request info for debugging ---
//             _logger.LogInformation("Request to {path} method {method}", resultContext.HttpContext.Request.Path, httpMethod);
//             var authHeader = resultContext.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
//             _logger.LogInformation("Authorization header: {auth}", authHeader ?? "None");

//             var userClaims = _httpContextAccessor.HttpContext?.User;

//             if (userClaims?.Identity?.IsAuthenticated == true)
//             {
//                 _logger.LogInformation("User is authenticated. Claims:");
//                 foreach (var c in userClaims.Claims)
//                 {
//                     _logger.LogInformation("  {type} = {value}", c.Type, c.Value);
//                 }
//             }
//             else
//             {
//                 _logger.LogInformation("User is NOT authenticated (no claims).");
//             }

//             // --- Determine username ---
//             string username = userClaims?.FindFirst("FullName")?.Value
//                               ?? userClaims?.FindFirst(ClaimTypes.Name)?.Value
//                               ?? userClaims?.FindFirst(ClaimTypes.Email)?.Value;

//             // --- Fallback to DB ---
//             if (string.IsNullOrEmpty(username) && userClaims != null)
//             {
//                 var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                 if (int.TryParse(userIdClaim, out int userId))
//                 {
//                     var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
//                     if (user != null)
//                         username = $"{user.FirstName} {user.LastName}";
//                 }
//             }

//             // --- Final fallback ---
//             if (string.IsNullOrEmpty(username))
//                 username = "System";

//             // --- Extract entity info ---
//             int entityId = 0;
//             string entityLabel = "";
//             foreach (var arg in context.ActionArguments.Values)
//             {
//                 switch (arg)
//                 {
//                     case HotelAPI.Models.Agency agency:
//                         entityId = agency.Id;
//                         entityLabel = agency.AgencyName;
//                         break;
//                     case HotelAPI.Models.HotelInfo hotel:
//                         entityId = hotel.Id;
//                         entityLabel = hotel.HotelName;
//                         break;
//                     case int id:
//                         entityId = id;
//                         break;
//                 }
//             }

//             // --- Create log entry ---
//             var log = new RecentActivity
//             {
//                 Username = username,
//                 ActionType = actionType,
//                 Entity = entityName,
//                 EntityId = entityId,
//                 Description = $"{username} {actionType.ToLower()} {entityName} \"{entityLabel}\"",
//                 CreatedAt = DateTime.UtcNow
//             };

//             _context.RecentActivities.Add(log);
//             await _context.SaveChangesAsync();
//         }
//     }
// }




















// new one 
// using HotelAPI.Data;
// using HotelAPI.Models;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc.Filters;
// using Microsoft.EntityFrameworkCore.ChangeTracking;
// using System;
// using System.Linq;
// using System.Threading.Tasks;

// namespace HotelAPI.Filters
// {
//     public class ActivityLogFilter : IAsyncActionFilter
//     {
//         private readonly AppDbContext _context;
//         private readonly IHttpContextAccessor _httpContextAccessor;

//         public ActivityLogFilter(AppDbContext context, IHttpContextAccessor httpContextAccessor)
//         {
//             _context = context;
//             _httpContextAccessor = httpContextAccessor;
//         }

//         // Get current user info
//         private (int userId, string userName) GetCurrentUser()
//         {
//             var httpContext = _httpContextAccessor.HttpContext;
//             if (httpContext != null && httpContext.User.Identity?.IsAuthenticated == true)
//             {
//                 var userId = int.Parse(httpContext.User.FindFirst("id")?.Value ?? "0");
//                 var userName = httpContext.User.Identity.Name ?? "Unknown";
//                 return (userId, userName);
//             }
//             return (1, "System"); // fallback for system actions
//         }

//         // Log a single activity
//         private async Task LogAsync(string action, string entity, int? entityId, string? description)
//         {
//             var (userId, userName) = GetCurrentUser();

//             var activity = new RecentActivity
//             {
//                 UserId = userId,
//                 UserName = userName,
//                 Action = action,
//                 Entity = entity,
//                 EntityId = entityId,
//                 Description = description,
//                 Timestamp = DateTime.UtcNow
//             };

//             _context.RecentActivities.Add(activity);
//             await _context.SaveChangesAsync();
//         }

//         // Log changes from DbContext entries
//         public async Task LogChangesAsync(ChangeTracker changeTracker)
//         {
//             var entries = changeTracker.Entries()
//                 .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added ||
//                             e.State == Microsoft.EntityFrameworkCore.EntityState.Modified ||
//                             e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);

//             foreach (var entry in entries)
//             {
//                 string action = entry.State switch
//                 {
//                     Microsoft.EntityFrameworkCore.EntityState.Added => "CREATE",
//                     Microsoft.EntityFrameworkCore.EntityState.Modified => "UPDATE",
//                     Microsoft.EntityFrameworkCore.EntityState.Deleted => "DELETE",
//                     _ => "UNKNOWN"
//                 };

//                 string entityName = entry.Entity.GetType().Name;
//                 int? entityId = entry.Property("Id")?.CurrentValue as int?;

//                 string description = null;

//                 if (action == "UPDATE")
//                 {
//                     description = string.Join(", ", entry.Properties
//                         .Where(p => p.IsModified)
//                         .Select(p => $"{p.Metadata.Name}: '{p.OriginalValue}' => '{p.CurrentValue}'"));
//                 }
//                 else if (action == "CREATE" || action == "DELETE")
//                 {
//                     var nameProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.ToLower().Contains("name"));
//                     description = nameProp != null ? $"{nameProp.CurrentValue} {action.ToLower()}d" : action;
//                 }

//                 await LogAsync(action, entityName, entityId, description);
//             }
//         }

//         // IAsyncActionFilter implementation
//         public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//         {
//             // Execute the action
//             var executedContext = await next();

//             // Log all changes after action execution
//             await LogChangesAsync(_context.ChangeTracker);
//         }
//     }
// }
