namespace FashionStore.Infrastructure
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddOptions<Images.CloudinarySettings>()
                .BindConfiguration(Images.CloudinarySettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddScoped<FashionStore.Infrastructure.Contracts.Abstractions.Images.ICloudinaryImageService,
                Images.CloudinaryImageService>();
            services.AddScoped<FashionStore.Infrastructure.Contracts.Abstractions.Videos.ICloudinaryVideoService,
                Images.CloudinaryVideoService>();
            services.AddScoped<FashionStore.Infrastructure.Contracts.Abstractions.Images.IImageProcessor,
                Images.ImageProcessor>();
            services.AddScoped<FashionStore.Infrastructure.Contracts.Abstractions.Encryption.IRsaEncryptionService,
                Security.RsaEncryptionService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.Categories.ICategoryRepository,
                Repository.CategoryRepo.CategoryRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.Brands.IBrandRepository,
                Repository.BrandRepo.BrandRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.MainCarousels.IMainCarouselRepository,
                Repository.MainCarouselRepo.MainCarouselRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.Products.IProductRepository,
                Repository.ProductRepo.ProductRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.PromotionBanners.IPromotionBannerRepository,
                Repository.PromotionBannerRepo.PromotionBannerRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.CatalogOptions.ICatalogOptionRepository,
                Repository.CatalogOptionRepo.CatalogOptionRepository>();
            services.AddScoped<FashionStore.Domain.Repositories.PromotionVideos.IPromotionVideoRepository,
                Repository.PromotionVideoRepo.PromotionVideoRepository>();

            services.AddScoped<IEmailNotificationService, Notification.EmailNotificationService>();
            services.AddScoped<IEmailTemplateRenderer, Notification.EmailTemplateRenderer>();
            services.AddHttpClient<IEmailProvider, Notification.MailgunEmailProvider>();
            services.AddSingleton<Messages.NotificationQueue.EmailNotificationQueueService>();
            services.AddSingleton<IEmailNotificationQueueService>(provider =>
                provider.GetRequiredService<Messages.NotificationQueue.EmailNotificationQueueService>());
            services.AddHostedService<Messages.NotificationQueue.EmailNotificationProcessorService>();
            services.AddHostedService<Messages.NotificationQueue.PendingEmailNotificationProcessorService>();
            return services;
        }
    }
}
