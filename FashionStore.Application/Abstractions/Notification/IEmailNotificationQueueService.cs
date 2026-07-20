using FashionStore.Domain.Entities;

namespace FashionStore.Application.Abstractions.Notification
{
    public interface IEmailNotificationQueueService
    {
        void Enqueue(Guid notificationId, EmailNotification notification);
    }
}
