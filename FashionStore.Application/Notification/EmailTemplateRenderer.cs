using FashionStore.Domain.Enums;
using Microsoft.AspNetCore.Hosting;

namespace FashionStore.Application.Notification
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly IWebHostEnvironment _environment;

        public EmailTemplateRenderer(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> RenderAsync(EmailNotificationTypeEnum templateType, IReadOnlyDictionary<string, string> tokens)
        {
            var templatePath = GetTemplatePath(templateType);
            var content = await File.ReadAllTextAsync(templatePath);

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
                _ => throw new ArgumentException($"No email template is configured for {templateType}.", nameof(templateType))
            };
        }
    }
}
