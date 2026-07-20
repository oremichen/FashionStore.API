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

        public NotificationRepository(FashionStoreDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<QueueEmailNotification> CreateProcessingAsync(
            EmailNotification notification,
            CancellationToken cancellationToken)
        {
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
            return queuedNotification;
        }

        public async Task UpdateStatusAsync(
            Guid id,
            string status,
            string? lastError,
            CancellationToken cancellationToken)
        {
            var queuedNotification = await _dbContext.QueueEmailNotifications
                .SingleAsync(item => item.Id == id, cancellationToken);

            queuedNotification.Status = status;
            queuedNotification.LastError = lastError;
            if (status == NotificationStatus.Failed)
            {
                queuedNotification.FailedAt = DateTimeOffset.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<QueueEmailNotification>> GetRecoverableAsync(
            int maxRetryCount,
            DateTimeOffset staleProcessingBefore,
            CancellationToken cancellationToken)
        {
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
        }
    }
}
