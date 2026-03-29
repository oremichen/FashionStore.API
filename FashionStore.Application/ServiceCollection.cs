using FashionStore.Application.Abstractions.Notification;
using FashionStore.Application.Messages.NotificationQueue;
using Microsoft.Extensions.DependencyInjection;

namespace FashionStore.Application
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<Abstractions.Auth.IAuthService, Features.Auth.AuthService>();
            services.AddScoped<Abstractions.Auth.ITokenService, Features.Auth.TokenService>();
            services.AddScoped<IEmailNotificationService, Features.Notification.EmailNotificationService>();
            services.AddScoped<IEmailTemplateRenderer, Features.Notification.EmailTemplateRenderer>();

            services.AddSingleton<EmailNotificationQueueService>();
            services.AddSingleton<IEmailNotificationQueueService>(sp => sp.GetRequiredService<EmailNotificationQueueService>());
            services.AddHostedService<EmailNotificationProcessorService>();

            return services;
        }
    }
}
