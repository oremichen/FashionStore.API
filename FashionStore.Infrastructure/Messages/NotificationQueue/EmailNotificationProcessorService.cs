namespace FashionStore.Infrastructure.Messages.NotificationQueue
{
    public class EmailNotificationProcessorService : BackgroundService
    {
        private readonly EmailNotificationQueueService _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailNotificationProcessorService> _logger;

        public EmailNotificationProcessorService(
            EmailNotificationQueueService queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailNotificationProcessorService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var queuedItem in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                var notification = queuedItem.Notification;
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                try
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                    var sendResult = await notificationService.SendEmailAsync(notification);
                    var status = sendResult.IsSuccessful
                        ? NotificationStatus.Completed
                        : NotificationStatus.Pending;

                    await repository.UpdateStatusAsync(
                        queuedItem.Id,
                        status,
                        sendResult.IsSuccessful ? null : sendResult.Description,
                        stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Initial notification delivery failed. Subject: {Subject}.",
                        notification.Subject);
                    await repository.UpdateStatusAsync(
                        queuedItem.Id,
                        NotificationStatus.Pending,
                        exception.Message,
                        stoppingToken);
                }
            }
        }
    }
}
