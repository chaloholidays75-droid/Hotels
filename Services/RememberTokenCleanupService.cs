using HotelAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelAPI.Services
{
    public class RememberTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RememberTokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(12); // run every 12 hours

        public RememberTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RememberTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 RememberToken cleanup service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;
                    var expiredTokens = await context.RememberTokens
                        .Where(t => t.Expiry < now || t.IsRevoked)
                        .ToListAsync(stoppingToken);

                    if (expiredTokens.Any())
                    {
                        context.RememberTokens.RemoveRange(expiredTokens);
                        var count = await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"🧽 Removed {count} expired or revoked remember tokens.");
                    }
                    else
                    {
                        _logger.LogInformation("✅ No expired remember tokens found.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during RememberToken cleanup.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("🛑 RememberToken cleanup service stopped.");
        }
    }
}
