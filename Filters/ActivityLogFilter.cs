using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        // Get current user info
        private (int userId, string userName) GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = int.Parse(httpContext.User.FindFirst("id")?.Value ?? "0");
                var userName = httpContext.User.Identity.Name ?? "Unknown";
                return (userId, userName);
            }
            return (1, "System"); // fallback for system actions
        }

        // Log a single activity
        private async Task LogAsync(string action, string entity, int? entityId, string? description)
        {
            var (userId, userName) = GetCurrentUser();

            var activity = new RecentActivity
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.RecentActivities.Add(activity);
            await _context.SaveChangesAsync();
        }

        // Log changes from DbContext entries
        public async Task LogChangesAsync(ChangeTracker changeTracker)
        {
            var entries = changeTracker.Entries()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added ||
                            e.State == Microsoft.EntityFrameworkCore.EntityState.Modified ||
                            e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);

            foreach (var entry in entries)
            {
                string action = entry.State switch
                {
                    Microsoft.EntityFrameworkCore.EntityState.Added => "CREATE",
                    Microsoft.EntityFrameworkCore.EntityState.Modified => "UPDATE",
                    Microsoft.EntityFrameworkCore.EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                string entityName = entry.Entity.GetType().Name;
                int? entityId = entry.Property("Id")?.CurrentValue as int?;

                string description = null;

                if (action == "UPDATE")
                {
                    description = string.Join(", ", entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p => $"{p.Metadata.Name}: '{p.OriginalValue}' => '{p.CurrentValue}'"));
                }
                else if (action == "CREATE" || action == "DELETE")
                {
                    var nameProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.ToLower().Contains("name"));
                    description = nameProp != null ? $"{nameProp.CurrentValue} {action.ToLower()}d" : action;
                }

                await LogAsync(action, entityName, entityId, description);
            }
        }

        // IAsyncActionFilter implementation
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Execute the action
            var executedContext = await next();

            // Log all changes after action execution
            await LogChangesAsync(_context.ChangeTracker);
        }
    }
}
