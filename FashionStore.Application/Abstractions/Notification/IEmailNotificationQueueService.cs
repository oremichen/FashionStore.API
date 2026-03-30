using FashionStore.Domain.Entities;

namespace FashionStore.Application.Abstractions.Notification
{
    public interface IEmailNotificationQueueService
    {
        public void Enqueue(EmailNotification notification);
    }
}
