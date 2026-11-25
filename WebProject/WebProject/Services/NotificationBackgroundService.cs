using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebProject.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<NotificationBackgroundService>
            _logger;

        private static readonly TimeSpan CheckInterval =
            TimeSpan.FromHours(1);

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Notification background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation(
                        "Starting automatic notification check.");

                    using var scope = _scopeFactory.CreateScope();

                    var notificationService = scope.ServiceProvider
                        .GetRequiredService<NotificationService>();

                    int notificationsCreated =
                        await notificationService
                            .GenerateNotificationsAsync();

                    _logger.LogInformation(
                        "Automatic notification check completed. " +
                        "{Count} new notifications created.",
                        notificationsCreated);

                    // Email processing will be added here later.
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred during the " +
                        "automatic notification check.");
                }

                try
                {
                    await Task.Delay(
                        CheckInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Notification background service stopped.");
        }
    }
}
