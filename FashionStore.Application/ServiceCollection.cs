using FashionStore.Application.Messages.NotificationQueue;
using FashionStore.Application.Notification;
using FashionStore.Application.Abstractions.Encryption;
using FashionStore.Application.Abstractions.Users;
using FashionStore.Application.Utils.Encryption;
using FashionStore.Application.Features.Users;

namespace FashionStore.Application
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<Abstractions.Auth.IAuthService, Features.Auth.AuthService>();
            services.AddScoped<Abstractions.Auth.ITokenService, Features.Auth.TokenService>();
            services.AddScoped<IRsaEncryptionService, RsaEncryptionService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

            services.AddSingleton<EmailNotificationQueueService>();
            services.AddSingleton<IEmailNotificationQueueService>(sp => sp.GetRequiredService<EmailNotificationQueueService>());
            services.AddHostedService<EmailNotificationProcessorService>();
            services.AddHostedService<PendingEmailNotificationProcessorService>();

            return services;
        }
    }
}
