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
            services.AddScoped<FashionStore.Application.Abstractions.Images.ICloudinaryImageService,
                Images.CloudinaryImageService>();
            services.AddScoped<FashionStore.Application.Abstractions.Videos.ICloudinaryVideoService,
                Images.CloudinaryVideoService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.Categories.ICategoryRepository,
                Repository.CategoryRepo.CategoryRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.Brands.IBrandRepository,
                Repository.BrandRepo.BrandRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.MainCarousels.IMainCarouselRepository,
                Repository.MainCarouselRepo.MainCarouselRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.Products.IProductRepository,
                Repository.ProductRepo.ProductRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.PromotionBanners.IPromotionBannerRepository,
                Repository.PromotionBannerRepo.PromotionBannerRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.CatalogOptions.ICatalogOptionRepository,
                Repository.CatalogOptionRepo.CatalogOptionRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.PromotionVideos.IPromotionVideoRepository,
                Repository.PromotionVideoRepo.PromotionVideoRepository>();
            return services;
        }
    }
}
