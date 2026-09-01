using FashionStore.Domain.Enums;
using Microsoft.AspNetCore.Hosting;

namespace FashionStore.Infrastructure.Notification
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _logoUrl;

        public EmailTemplateRenderer(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            var assetsBaseUrl = configuration["EmailTemplates:AssetsBaseUrl"]
                ?? throw new InvalidOperationException("EmailTemplates:AssetsBaseUrl is not configured.");
            _logoUrl = $"{assetsBaseUrl.TrimEnd('/')}/images/lg.png";
        }

        public async Task<string> RenderAsync(EmailNotificationTypeEnum templateType, IReadOnlyDictionary<string, string> tokens)
        {
            var templatePath = GetTemplatePath(templateType);
            var content = await File.ReadAllTextAsync(templatePath);
            content = content.Replace("{{logoUrl}}", _logoUrl, StringComparison.OrdinalIgnoreCase);

            foreach (var token in tokens)
            {
                content = content.Replace($"{{{{{token.Key}}}}}", token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return content;
        }

        private string GetTemplatePath(EmailNotificationTypeEnum templateType)
        {
            return templateType switch
            {
                EmailNotificationTypeEnum.Registration => Path.Combine(_environment.WebRootPath, "EmailTemplates", "RegisterTemplate.html"),
                EmailNotificationTypeEnum.Confirmation => Path.Combine(_environment.WebRootPath, "EmailTemplates", "ConfirmationTemplate.html"),
                EmailNotificationTypeEnum.ForgotPassword => Path.Combine(_environment.WebRootPath, "EmailTemplates", "ForgotPasswordTemplate.html"),
                EmailNotificationTypeEnum.UserCreation => Path.Combine(_environment.WebRootPath, "EmailTemplates", "UserCreationTemplate.html"),
                EmailNotificationTypeEnum.ContactRecipient => Path.Combine(_environment.WebRootPath, "EmailTemplates", "ContactRecipientTemplate.html"),
                EmailNotificationTypeEnum.ContactCustomer => Path.Combine(_environment.WebRootPath, "EmailTemplates", "ContactCustomerTemplate.html"),
                EmailNotificationTypeEnum.UserStatusChanged => Path.Combine(_environment.WebRootPath, "EmailTemplates", "UserStatusChangedTemplate.html"),
                _ => throw new ArgumentException($"No email template is configured for {templateType}.", nameof(templateType))
            };
        }
    }
}
