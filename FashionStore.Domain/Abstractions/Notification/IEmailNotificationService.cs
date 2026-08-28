using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Notification
{
    public interface IEmailNotificationService
    {
        Task QueueEmailAsync(
            EmailNotification notification,
            CancellationToken cancellationToken = default);

        Task<EmailDeliveryResult> SendEmailAsync(EmailNotification notification);
    }

    public sealed record EmailDeliveryResult(bool IsSuccessful, string Description)
    {
        public static EmailDeliveryResult Success(string description)
        {
            return new(true, description);
        }
        public static EmailDeliveryResult Failure(string description)
        {
            return new(false, description);
        }
    }
}
