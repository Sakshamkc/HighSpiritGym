using HighSpiritApp.DataContext;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Services
{
    public class AutoPunchOutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoPunchOutService> _logger;
        private static readonly TimeSpan NepalOffset = new TimeSpan(5, 45, 0);
        private static readonly TimeSpan MaxGymDuration = TimeSpan.FromHours(3);

        public AutoPunchOutService(IServiceProvider serviceProvider, ILogger<AutoPunchOutService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AutoPunchOut();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in auto punch-out service");
                }

                // Check every 15 minutes
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task AutoPunchOut()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();

            var nepalNow = DateTime.UtcNow.Add(NepalOffset);
            var cutoff = nepalNow.Add(-MaxGymDuration);

            // Find all records that are still "in" but checked in more than 3 hours ago
            var overdueRecords = await context.Attendances
                .Where(a => a.CheckOutTime == null && a.CheckInTime <= cutoff)
                .ToListAsync();

            if (overdueRecords.Count > 0)
            {
                foreach (var record in overdueRecords)
                {
                    // Set checkout to checkin + 3 hours
                    record.CheckOutTime = record.CheckInTime.Add(MaxGymDuration);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Auto punched out {Count} overdue records", overdueRecords.Count);
            }
        }
    }
}
