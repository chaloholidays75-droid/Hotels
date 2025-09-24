using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HotelAPI.Services
{
    public interface IActivityLoggerService
    {
        Task LogChangesAsync(ChangeTracker changeTracker, int userId, string userName);
    }
}
