using Microsoft.EntityFrameworkCore;
using PRM_PRJ.Models;

namespace PRM_PRJ.Workers
{
    public class RefreshTokenCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupWorker> _logger;

        public RefreshTokenCleanupWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
     CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                    var affected = await db.RefreshTokens
                        .Where(x =>
                            !x.IsRevoked &&
                            x.ExpiryDate < DateTime.UtcNow)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(
                                p => p.IsRevoked,
                                true),
                            stoppingToken);

                    if (affected > 0)
                    {
                        _logger.LogInformation(
                            $"Revoked {affected} expired refresh tokens");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning refresh tokens");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(30),
                    stoppingToken);
            }
        }
    }
}