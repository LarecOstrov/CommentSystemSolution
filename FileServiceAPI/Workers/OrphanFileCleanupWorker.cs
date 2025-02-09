using FileServiceAPI.Services.Interfaces;
using Serilog;

namespace FileServiceAPI.Workers;

internal class OrphanFileCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Once a day

    public OrphanFileCleanupWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IOrphanFileCleanupService>();
                await cleanupService.CleanupOrphanFilesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while cleaning orphan files.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}
