using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Notification;

public interface INotificationRepository
{
    Task<QueueEmailNotification> CreateProcessingAsync(EmailNotification notification, CancellationToken cancellationToken);
    Task UpdateStatusAsync(Guid id, string status, string? lastError, CancellationToken cancellationToken);
    Task<IReadOnlyList<QueueEmailNotification>> GetRecoverableAsync(int maxRetryCount, DateTimeOffset staleProcessingBefore, CancellationToken cancellationToken);
    Task RecordRetryResultAsync(Guid id, bool delivered, string? lastError, int maxRetryCount, CancellationToken cancellationToken);
}
