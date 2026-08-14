using System.Text.Json;
using FashionStore.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.NotificationRepo
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly FashionStoreDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(
            FashionStoreDbContext dbContext,
            IConfiguration configuration,
            ILogger<NotificationRepository> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<QueueEmailNotification> CreateProcessingAsync(
            EmailNotification notification,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Persisting email notification for processing.");
            var queuedNotification = new QueueEmailNotification
            {
                From = string.IsNullOrWhiteSpace(notification.From)
                    ? _configuration["MailSettings:DefaultFromAddress"]
                    : notification.From,
                ToRecipients = JsonSerializer.Serialize(notification.To ?? []),
                CcRecipients = notification.Cc == null ? null : JsonSerializer.Serialize(notification.Cc),
                BccRecipients = notification.Bcc == null ? null : JsonSerializer.Serialize(notification.Bcc),
                Subject = notification.Subject,
                Body = notification.Body,
                RetryCount = 0,
                Status = NotificationStatus.Processing
            };

            _dbContext.QueueEmailNotifications.Add(queuedNotification);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Persisted email notification {NotificationId} for processing.", queuedNotification.Id);
            return queuedNotification;
        }

        public async Task UpdateStatusAsync(
            Guid id,
            string status,
            string? lastError,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating notification {NotificationId} to status {Status}.", id, status);
            var queuedNotification = await _dbContext.QueueEmailNotifications
                .SingleAsync(item => item.Id == id, cancellationToken);

            queuedNotification.Status = status;
            queuedNotification.LastError = lastError;
            if (status == NotificationStatus.Failed)
            {
                queuedNotification.FailedAt = DateTimeOffset.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated notification {NotificationId} to status {Status}.", id, status);
        }

        public async Task<IReadOnlyList<QueueEmailNotification>> GetRecoverableAsync(
            int maxRetryCount,
            DateTimeOffset staleProcessingBefore,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying recoverable notifications with max retry count {MaxRetryCount}.", maxRetryCount);
            return await _dbContext.QueueEmailNotifications
                .Where(item => (item.Status == NotificationStatus.Pending ||
                                (item.Status == NotificationStatus.Processing &&
                                 item.CreatedAt <= staleProcessingBefore)) &&
                               item.RetryCount < maxRetryCount)
                .OrderBy(item => item.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        public async Task RecordRetryResultAsync(
            Guid id,
            bool delivered,
            string? lastError,
            int maxRetryCount,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recording retry result for notification {NotificationId}; delivered: {Delivered}.", id, delivered);
            var queuedNotification = await _dbContext.QueueEmailNotifications
                .SingleAsync(item => item.Id == id, cancellationToken);

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

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Recorded retry result for notification {NotificationId}; status: {Status}, retry count: {RetryCount}.",
                id,
                queuedNotification.Status,
                queuedNotification.RetryCount);
        }
    }
}
