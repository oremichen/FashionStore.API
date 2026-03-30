using System.Text.Json;
using FashionStore.Shared.Constants;
using Microsoft.Extensions.Configuration;

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

        public async Task SaveFailedAsync(EmailNotification notification, int retryCount, string? lastError, CancellationToken cancellationToken)
        {
            _dbContext.FailedEmailNotifications.Add(new FailedEmailNotification
            {
                From = _configuration["MailSettings:DefaultFromAddress"],
                ToRecipients = JsonSerializer.Serialize(notification.To ?? []),
                CcRecipients = notification.Cc == null ? null : JsonSerializer.Serialize(notification.Cc),
                BccRecipients = notification.Bcc == null ? null : JsonSerializer.Serialize(notification.Bcc),
                Subject = notification.Subject,
                Body = notification.Body,
                RetryCount = retryCount,
                LastError = lastError,
                Status = FailedNotificationStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
