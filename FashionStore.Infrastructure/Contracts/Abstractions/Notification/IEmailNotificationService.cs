using FashionStore.Domain.Entities;
using FashionStore.Shared.Common;

namespace FashionStore.Infrastructure.Contracts.Abstractions.Notification
{
    public interface IEmailNotificationService
    {
        Task QueueEmailAsync(
            EmailNotification notification,
            CancellationToken cancellationToken = default);

        Task<ResponseResult> SendEmailAsync(EmailNotification notification);
    }
}
