using FashionStore.Application.Messages.NotificationQueue;
using FashionStore.Application.Notification;
using FashionStore.Application.Abstractions.Encryption;
using FashionStore.Application.Abstractions.Users;
using FashionStore.Application.Utils.Encryption;
using FashionStore.Application.Features.Users;
using FashionStore.Application.Abstractions.Categories;
using FashionStore.Application.Features.Categories;
using FashionStore.Application.Abstractions.Brands;
using FashionStore.Application.Features.Brands;
using FashionStore.Application.Abstractions.MainCarousels;
using FashionStore.Application.Features.MainCarousels;
using FashionStore.Application.Abstractions.Images;
using FashionStore.Application.Features.Images;
using FashionStore.Application.Abstractions.Products;
using FashionStore.Application.Features.Products;
using FashionStore.Application.Abstractions.PromotionBanners;
using FashionStore.Application.Features.PromotionBanners;
using FashionStore.Application.Abstractions.CatalogOptions;
using FashionStore.Application.Features.CatalogOptions;

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
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IMainCarouselService, MainCarouselService>();
            services.AddScoped<IImageProcessor, ImageProcessor>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IPromotionBannerService, PromotionBannerService>();
            services.AddScoped<ICatalogOptionService, CatalogOptionService>();
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
            services.AddHttpClient<IEmailProvider, MailgunEmailProvider>();

            services.AddSingleton<EmailNotificationQueueService>();
            services.AddSingleton<IEmailNotificationQueueService>(sp => sp.GetRequiredService<EmailNotificationQueueService>());
            services.AddHostedService<EmailNotificationProcessorService>();
            services.AddHostedService<PendingEmailNotificationProcessorService>();

            return services;
        }
    }
}
