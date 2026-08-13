namespace FashionStore.Infrastructure
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.Categories.ICategoryRepository,
                Repository.CategoryRepo.CategoryRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.Brands.IBrandRepository,
                Repository.BrandRepo.BrandRepository>();
            services.AddScoped<FashionStore.Application.Abstractions.MainCarousels.IMainCarouselRepository,
                Repository.MainCarouselRepo.MainCarouselRepository>();
            return services;
        }
    }
}
