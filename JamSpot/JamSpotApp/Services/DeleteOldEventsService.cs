using JamSpotApp.Data;
using Microsoft.EntityFrameworkCore;

public class DeleteOldEventsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public DeleteOldEventsService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

            // Изчакване от 24 часа преди следващото изпълнение
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task DeletePastEventsAsync(JamSpotDbContext context)
    {
        var yesterday = DateTime.Today.AddDays(-1);
        var pastEvents = await context.Events
            .Where(e => e.Date < yesterday)
            .ToListAsync();

        if (pastEvents.Any())
        {
            context.Events.RemoveRange(pastEvents);
            await context.SaveChangesAsync();
        }
    }
}
