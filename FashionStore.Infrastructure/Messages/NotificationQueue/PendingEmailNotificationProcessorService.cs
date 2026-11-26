using System.Text.Json;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Messages.NotificationQueue
{
    public class PendingEmailNotificationProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbContextFactory<FashionStoreDbContext> _dbContextFactory;
        private readonly ILogger<PendingEmailNotificationProcessorService> _logger;
        private readonly IConfiguration _configuration;

        public PendingEmailNotificationProcessorService(
            IServiceScopeFactory scopeFactory,
            IDbContextFactory<FashionStoreDbContext> dbContextFactory,
            ILogger<PendingEmailNotificationProcessorService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _dbContextFactory = dbContextFactory;
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
                try
                {
                    await ProcessPendingNotificationsAsync(
                        maxRetryCount,
                        processingRecoveryDelay,
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Pending notification recovery failed. The background service will continue on the next polling cycle.");
                }
            }
            while (await WaitForNextPollingCycleAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextPollingCycleAsync(
            PeriodicTimer timer,
            CancellationToken cancellationToken)
        {
            try
            {
                return await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        private async Task ProcessPendingNotificationsAsync(
            int maxRetryCount,
            TimeSpan processingRecoveryDelay,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var staleProcessingBefore = DateTimeOffset.UtcNow.Subtract(processingRecoveryDelay);
            _logger.LogDebug(
                "Loading recoverable email notifications with maximum retry count {MaxRetryCount}.",
                maxRetryCount);
            var pendingNotifications = await dbContext.QueueEmailNotifications
                .Where(item => (item.Status == NotificationStatus.Pending ||
                                (item.Status == NotificationStatus.Processing &&
                                 item.CreatedAt <= staleProcessingBefore)) &&
                               item.RetryCount < maxRetryCount)
                .OrderBy(item => item.CreatedAt)
                .Take(100)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var queuedNotification in pendingNotifications)
            {
                var delivered = false;
                string? lastError = null;

                try
                {
                    await UpdateStatusAsync(
                        dbContext,
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

                await RecordRetryResultAsync(
                    dbContext,
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

        private static async Task UpdateStatusAsync(
            FashionStoreDbContext dbContext,
            Guid notificationId,
            string status,
            string? lastError,
            CancellationToken cancellationToken)
        {
            var queuedNotification = await dbContext.QueueEmailNotifications
                .SingleAsync(item => item.Id == notificationId, cancellationToken);

            queuedNotification.Status = status;
            queuedNotification.LastError = lastError;
            if (status == NotificationStatus.Failed)
            {
                queuedNotification.FailedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static async Task RecordRetryResultAsync(
            FashionStoreDbContext dbContext,
            Guid notificationId,
            bool delivered,
            string? lastError,
            int maxRetryCount,
            CancellationToken cancellationToken)
        {
            var queuedNotification = await dbContext.QueueEmailNotifications
                .SingleAsync(item => item.Id == notificationId, cancellationToken);

            queuedNotification.RetryCount++;
            queuedNotification.LastError = delivered ? null : lastError;
            queuedNotification.Status = delivered
                ? NotificationStatus.Completed
                : queuedNotification.RetryCount >= maxRetryCount
                    ? NotificationStatus.Failed
                    : NotificationStatus.Pending;

            if (queuedNotification.Status == NotificationStatus.Failed)
            {
                queuedNotification.FailedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
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
