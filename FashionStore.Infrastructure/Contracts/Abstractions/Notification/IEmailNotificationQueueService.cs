using FashionStore.Domain.Entities;

namespace FashionStore.Infrastructure.Contracts.Abstractions.Notification
{
    public interface IEmailNotificationQueueService
    {
        void Enqueue(Guid notificationId, EmailNotification notification);
    }
}
