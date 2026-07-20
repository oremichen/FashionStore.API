using System.Text.Json;

namespace FashionStore.Application.Messages.NotificationQueue
{
    public class PendingEmailNotificationProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PendingEmailNotificationProcessorService> _logger;
        private readonly IConfiguration _configuration;

        public PendingEmailNotificationProcessorService(
            IServiceScopeFactory scopeFactory,
            ILogger<PendingEmailNotificationProcessorService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var maxRetryCount = Math.Max(
                1,
                _configuration.GetValue("NotificationSettings:RetryCount", 3));
            var pollingInterval = TimeSpan.FromSeconds(Math.Max(
                1,
                _configuration.GetValue("NotificationSettings:RetryDelaySeconds", 5)));
            var processingRecoveryDelay = TimeSpan.FromSeconds(Math.Max(
                1,
                _configuration.GetValue("NotificationSettings:ProcessingRecoveryDelaySeconds", 60)));

            using var timer = new PeriodicTimer(pollingInterval);
            do
            {
                await ProcessPendingNotificationsAsync(
                    maxRetryCount,
                    processingRecoveryDelay,
                    stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task ProcessPendingNotificationsAsync(
            int maxRetryCount,
            TimeSpan processingRecoveryDelay,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
            var pendingNotifications = await repository.GetRecoverableAsync(
                maxRetryCount,
                DateTimeOffset.UtcNow.Subtract(processingRecoveryDelay),
                cancellationToken);

            foreach (var queuedNotification in pendingNotifications)
            {
                var delivered = false;
                string? lastError = null;

                try
                {
                    await repository.UpdateStatusAsync(
                        queuedNotification.Id,
                        NotificationStatus.Processing,
                        queuedNotification.LastError,
                        cancellationToken);

                    var sendResult = await notificationService.SendEmailAsync(ToEmailNotification(queuedNotification));
                    delivered = sendResult.IsSuccessful;
                    lastError = delivered ? null : sendResult.Description;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    lastError = exception.Message;
                    _logger.LogError(
                        exception,
                        "Retry delivery failed for queued notification {NotificationId}.",
                        queuedNotification.Id);
                }

                await repository.RecordRetryResultAsync(
                    queuedNotification.Id,
                    delivered,
                    lastError,
                    maxRetryCount,
                    cancellationToken);

                _logger.LogInformation(
                    "Processed retry {RetryCount} of {MaxRetryCount} for queued notification {NotificationId}. Delivered: {Delivered}.",
                    queuedNotification.RetryCount + 1,
                    maxRetryCount,
                    queuedNotification.Id,
                    delivered);
            }
        }

        private static EmailNotification ToEmailNotification(QueueEmailNotification queuedNotification)
        {
            return new EmailNotification
            {
                From = queuedNotification.From!,
                To = DeserializeRecipients(queuedNotification.ToRecipients),
                Cc = DeserializeRecipients(queuedNotification.CcRecipients),
                Bcc = DeserializeRecipients(queuedNotification.BccRecipients),
                Subject = queuedNotification.Subject,
                Body = queuedNotification.Body,
                Attchements = []
            };
        }

        private static List<string> DeserializeRecipients(string? recipients)
        {
            return string.IsNullOrWhiteSpace(recipients)
                ? []
                : JsonSerializer.Deserialize<List<string>>(recipients) ?? [];
        }
    }
}
