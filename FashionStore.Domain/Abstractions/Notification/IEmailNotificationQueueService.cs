using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Notification
{
    public interface IEmailNotificationQueueService
    {
        void Enqueue(Guid notificationId, EmailNotification notification);
    }
}
