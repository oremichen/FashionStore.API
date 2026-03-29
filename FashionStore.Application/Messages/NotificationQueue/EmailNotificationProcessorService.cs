namespace FashionStore.Application.Messages.NotificationQueue
{
    public class EmailNotificationProcessorService : BackgroundService
    {
        private readonly EmailNotificationQueueService _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailNotificationProcessorService> _logger;
        private readonly IConfiguration _configuration;

        public EmailNotificationProcessorService(
            EmailNotificationQueueService queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailNotificationProcessorService> logger,
            IConfiguration configuration)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var maxRetryAttempts = Math.Max(1, _configuration.GetValue("NotificationSettings:RetryCount", 3));
            var retryDelaySeconds = Math.Max(1, _configuration.GetValue("NotificationSettings:RetryDelaySeconds", 5));

            await foreach (var notification in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                var delivered = false;
                string? lastError = null;

                for (var attempt = 1; attempt <= maxRetryAttempts && !delivered; attempt++)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                        var sendResult = await notificationService.SendEmailAsync(notification);

                        if (sendResult.IsSuccessful)
                        {
                            delivered = true;
                            _logger.LogInformation(
                                "Processed notification successfully on attempt {Attempt}. Subject: {Subject}.",
                                attempt,
                                notification.Subject);
                            break;
                        }

                        lastError = sendResult.Description;
                        _logger.LogWarning(
                            "Notification delivery failed on attempt {Attempt} of {MaxAttempts}. Subject: {Subject}. Error: {Error}.",
                            attempt,
                            maxRetryAttempts,
                            notification.Subject,
                            lastError);
                    }
                    catch (Exception exception)
                    {
                        lastError = exception.Message;
                        _logger.LogError(
                            exception,
                            "Notification processing threw an exception on attempt {Attempt} of {MaxAttempts}. Subject: {Subject}.",
                            attempt,
                            maxRetryAttempts,
                            notification.Subject);
                    }

                    if (!delivered && attempt < maxRetryAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                    }
                }

                if (!delivered)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var failedNotificationStore = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    await failedNotificationStore.SaveFailedAsync(notification, maxRetryAttempts, lastError, stoppingToken);

                    _logger.LogError(
                        "Notification was persisted as failed after {RetryCount} attempts. Subject: {Subject}. Error: {Error}.",
                        maxRetryAttempts,
                        notification.Subject,
                        lastError);
                }
            }
        }
    }
}
