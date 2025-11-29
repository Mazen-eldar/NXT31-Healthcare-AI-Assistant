using Base.DAL.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.HangFireJobs
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceProvider sp, ILogger<RefreshTokenCleanupService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var cutoff = DateTime.UtcNow.AddDays(-90); // example policy
                    var old = await db.RefreshTokens
                                      .Where(t => t.ExpiresAtUtc < cutoff || t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff)
                                      .ToListAsync(stoppingToken);
                    if (old.Count > 0)
                    {
                        db.RefreshTokens.RemoveRange(old);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during refresh token cleanup");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }

}
