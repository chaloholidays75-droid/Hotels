using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HotelAPI.Services
{
    public class ActivityLoggerService : IActivityLoggerService
    {
        private readonly AppDbContext _context;

        public ActivityLoggerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string entity, int? entityId, string? description)
        {
            var activity = new RecentActivity
            {
                UserId = 0,
                UserName = "Unknown",
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.RecentActivities.Add(activity);
            await _context.SaveChangesAsync();
        }

        public async Task LogChangesAsync(ChangeTracker changeTracker, int userId, string userName)
        {
            var entries = changeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                string action = entry.State switch
                {
                    EntityState.Added => "CREATE",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                string entityName = entry.Entity.GetType().Name;
                int? entityId = entry.Properties.Any(p => p.Metadata.Name == "Id")
                                ? entry.Property("Id").CurrentValue as int?
                                : null;

                string? description = null;

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

                var activity = new RecentActivity
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    Entity = entityName,
                    EntityId = entityId,
                    Description = description,
                    Timestamp = DateTime.UtcNow
                };

                _context.RecentActivities.Add(activity);
            }

            await _context.SaveChangesAsync();
        }
    }
}
