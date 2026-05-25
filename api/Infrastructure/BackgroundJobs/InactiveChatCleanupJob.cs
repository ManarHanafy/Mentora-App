namespace api.Infrastructure.BackgroundJobs;

public class InactiveChatCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InactiveChatCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                var endedCount = await chatService.EndInactiveChatsAsync(10, stoppingToken);

                if (endedCount > 0)
                    logger.LogInformation("Ended {Count} inactive chats.", endedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running inactive chat cleanup job.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
