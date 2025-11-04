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

        public async Task LogChangesAsync(ChangeTracker changeTracker, int userId, string userName)
        {
            // Get all tracked changes (Added, Modified, Deleted)
            var entries = changeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                string tableName = entry.Entity.GetType().Name;
                string action = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                int recordId = 0;
                try
                {
                    // Safely get "Id" if present in entity
                    var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
                    if (idProperty != null && idProperty.CurrentValue != null)
                        recordId = Convert.ToInt32(idProperty.CurrentValue);
                }
                catch
                {
                    recordId = 0;
                }

                // Build a detailed change description
                string changes = string.Empty;
                if (entry.State == EntityState.Modified)
                {
                    var modifiedProps = entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p =>
                            $"{p.Metadata.Name}: '{p.OriginalValue}' → '{p.CurrentValue}'");
                    changes = string.Join("; ", modifiedProps);
                }
                else if (entry.State == EntityState.Added)
                {
                    changes = "New record created.";
                }
                else if (entry.State == EntityState.Deleted)
                {
                    changes = "Record deleted.";
                }

                var description = $"{action} operation on {tableName} (ID {recordId}) by {userName}. {changes}";

                var activity = new RecentActivity
                {
                    UserId = userId,
                    UserName = userName,
                    ActionType = action,
                    TableName = tableName,
                    RecordId = recordId,
                    Description = description,
                    ChangedData = entry.State == EntityState.Modified
                        ? string.Join("; ", entry.Properties.Where(p => p.IsModified)
                            .Select(p => $"{p.Metadata.Name}: {p.OriginalValue} → {p.CurrentValue}"))
                        : null,
                    Timestamp = DateTime.UtcNow
                };

                _context.RecentActivities.Add(activity);
            }

            await _context.SaveChangesAsync();
        }
    }
}
