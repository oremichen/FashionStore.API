using FashionStore.Domain.Enums;

namespace FashionStore.Infrastructure.Contracts.Abstractions.Notification
{
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderAsync(EmailNotificationTypeEnum templateType, IReadOnlyDictionary<string, string> tokens);
    }
}
