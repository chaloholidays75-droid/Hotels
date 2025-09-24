// using HotelAPI.Services;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc.Filters;
// using Microsoft.EntityFrameworkCore;
// using System.Threading.Tasks;

// namespace HotelAPI.Filters
// {
//     public class ActivityLogFilter : IAsyncActionFilter
//     {
//         private readonly IActivityLoggerService _logger;
//         private readonly IHttpContextAccessor _httpContextAccessor;

//         public ActivityLogFilter(IActivityLoggerService logger, IHttpContextAccessor httpContextAccessor)
//         {
//             _logger = logger;
//             _httpContextAccessor = httpContextAccessor;
//         }

//         public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//         {
//             var executedContext = await next();

//             var user = _httpContextAccessor.HttpContext?.User;
//             int userId = user != null && int.TryParse(user.FindFirst("id")?.Value, out var id) ? id : 0;
//             string userName = user?.Identity?.Name ?? "Unknown";

//             if (executedContext.HttpContext.RequestServices.GetService(typeof(DbContext)) is DbContext db)
//             {
//                 await _logger.LogChangesAsync(db.ChangeTracker, userId, userName);
//             }
//         }
//     }
// }
using HotelAPI.Data;
using HotelAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HotelAPI.Filters
{
    public class ActivityLogFilter : IAsyncActionFilter
    {
        private readonly IActivityLoggerService _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogFilter(IActivityLoggerService logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

   try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var user = httpContext?.User;

                // Extract user info from claims (default to 0/System if not logged in)
                int userId = user != null && int.TryParse(user.FindFirst("id")?.Value, out var id) ? id : 0;
                string userName = user?.Identity?.Name ?? "System";

                // Get DbContext from DI
                if (executedContext.HttpContext.RequestServices.GetService(typeof(AppDbContext)) is AppDbContext db)
                {
                    await _logger.LogChangesAsync(db.ChangeTracker, userId, userName);
                }
            }
            catch (Exception ex)
            {
                // Don’t let logging failures break the request
                Console.WriteLine($"[ActivityLogFilter] Logging failed: {ex.Message}");
            }
        }
    }
}
