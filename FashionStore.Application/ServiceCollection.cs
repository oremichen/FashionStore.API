using FashionStore.Application.Messages.NotificationQueue;
using FashionStore.Application.Notification;
using FashionStore.Application.Abstractions.Encryption;
using FashionStore.Application.Utils.Encryption;

namespace FashionStore.Application
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<Abstractions.Auth.IAuthService, Features.Auth.AuthService>();
            services.AddScoped<Abstractions.Auth.ITokenService, Features.Auth.TokenService>();
            services.AddScoped<IRsaEncryptionService, RsaEncryptionService>();
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

            services.AddSingleton<EmailNotificationQueueService>();
            services.AddSingleton<IEmailNotificationQueueService>(sp => sp.GetRequiredService<EmailNotificationQueueService>());
            services.AddHostedService<EmailNotificationProcessorService>();

            return services;
        }
    }
}
