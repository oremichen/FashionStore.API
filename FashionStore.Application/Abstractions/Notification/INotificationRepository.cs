using FashionStore.Domain.Entities;

namespace FashionStore.Application.Abstractions.Notification
{
    public interface INotificationRepository
    {
        Task SaveFailedAsync(EmailNotification notification, int retryCount, string? lastError, CancellationToken cancellationToken);
    }
}
