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

            var user = _httpContextAccessor.HttpContext?.User;
            int userId = user != null && int.TryParse(user.FindFirst("id")?.Value, out var id) ? id : 0;
            string userName = user?.Identity?.Name ?? "System";

            if (executedContext.HttpContext.RequestServices.GetService(typeof(DbContext)) is DbContext db)
            {
                await _logger.LogChangesAsync(db.ChangeTracker, userId, userName);
            }
        }
    }
}
