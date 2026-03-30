using FashionStore.Domain.Entities;
using FashionStore.Shared.Common;

namespace FashionStore.Application.Abstractions.Notification
{
    public interface IEmailNotificationService
    {
        Task<ResponseResult> SendEmailAsync(EmailNotification notification);
    }
}
