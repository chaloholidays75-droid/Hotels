// using HotelAPI.Data;
// using HotelAPI.Models;
// using Microsoft.EntityFrameworkCore;
// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore.ChangeTracking;
// namespace HotelAPI.Services
// {
//     public class ActivityLoggerService : IActivityLoggerService
//     {
//         private readonly AppDbContext _context;

//         public ActivityLoggerService(AppDbContext context)
//         {
//             _context = context;
//         }

//         public async Task LogChangesAsync(ChangeTracker changeTracker, int userId, string userName)
//         {
//             var entries = changeTracker.Entries()
//                                        .Where(e => e.State == EntityState.Added 
//                                                 || e.State == EntityState.Modified 
//                                                 || e.State == EntityState.Deleted);

//             foreach (var entry in entries)
//             {
//                 var entityName = entry.Entity.GetType().Name;
//                 string action = entry.State.ToString(); // Added, Modified, Deleted
//                 string description = $"Entity {entityName} has been {action}";

//                 var activity = new RecentActivity
//                 {
//                     UserId = userId,
//                     UserName = userName,
//                     Entity = entityName,
//                     EntityId = entry.CurrentValues.TryGetValue<int>("Id", out var id) ? id : 0,
//                     Action = action,
//                     Description = description,
//                     Timestamp = DateTime.UtcNow
//                 };

//                 _context.RecentActivities.Add(activity);
//             }

//             await _context.SaveChangesAsync();
//         }
//     }
// }
