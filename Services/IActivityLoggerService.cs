using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading.Tasks;

namespace HotelAPI.Services
{
    public interface IActivityLoggerService
    {
        Task LogAsync(string action, string entity, int? entityId, string? description);
        Task LogChangesAsync(ChangeTracker changeTracker, int userId, string userName);
    }
}
