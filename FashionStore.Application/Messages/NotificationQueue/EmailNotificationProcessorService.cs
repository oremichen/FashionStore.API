namespace FashionStore.Application.Messages.NotificationQueue
{
    public class EmailNotificationProcessorService : BackgroundService
    {
        private readonly EmailNotificationQueueService _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailNotificationProcessorService> _logger;

        public EmailNotificationProcessorService(EmailNotificationQueueService queue, IServiceScopeFactory scopeFactory, ILogger<EmailNotificationProcessorService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var notification in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

                    await notificationService.SendEmailAsync(notification);

                    _logger.LogInformation("Process notification event: {@notification}", notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process notification event: {@notification}", notification);
                }
            }
        }
    }

}
