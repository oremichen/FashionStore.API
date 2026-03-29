using System.Text.Json;
using FashionStore.Application.Abstractions.Notification;
using FashionStore.Infrastructure.Data;

namespace FashionStore.Infrastructure.Repository.NotificationRepo
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly FashionStoreDbContext _dbContext;

        public NotificationRepository(FashionStoreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveFailedAsync(EmailNotification notification, int retryCount, string? lastError, CancellationToken cancellationToken)
        {
            _dbContext.FailedEmailNotifications.Add(new FailedEmailNotification
            {
                From = notification.From,
                ToRecipients = JsonSerializer.Serialize(notification.To ?? []),
                CcRecipients = notification.Cc == null ? null : JsonSerializer.Serialize(notification.Cc),
                BccRecipients = notification.Bcc == null ? null : JsonSerializer.Serialize(notification.Bcc),
                Subject = notification.Subject,
                Body = notification.Body,
                RetryCount = retryCount,
                LastError = lastError,
                Status = "Failed",
                FailedAt = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
