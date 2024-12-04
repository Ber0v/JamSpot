using JamSpotApp.Data;
using Microsoft.EntityFrameworkCore;

namespace JamSpotApp.Services
{
    public class DeleteOldEventsService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _delay;

        public DeleteOldEventsService(IServiceProvider serviceProvider, TimeSpan? delay = null)
        {
            _serviceProvider = serviceProvider;
            _delay = delay ?? TimeSpan.FromDays(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<JamSpotDbContext>();
                    await DeletePastEventsAsync(context);
                }

                try
                {
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task DeletePastEventsAsync(JamSpotDbContext context)
        {
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var pastEvents = await context.Events
                .Where(e => e.Date < thirtyDaysAgo)
                .ToListAsync();

            if (pastEvents.Any())
            {
                context.Events.RemoveRange(pastEvents);
                await context.SaveChangesAsync();
            }
        }
    }
}