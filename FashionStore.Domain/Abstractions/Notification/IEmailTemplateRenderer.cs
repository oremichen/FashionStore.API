using FashionStore.Domain.Enums;

namespace FashionStore.Domain.Abstractions.Notification
{
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderAsync(EmailNotificationTypeEnum templateType, IReadOnlyDictionary<string, string> tokens);
    }
}
