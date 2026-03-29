using Microsoft.Extensions.DependencyInjection;

namespace FashionStore.Application
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<Abstractions.Auth.IAuthService, Features.Auth.AuthService>();
            services.AddScoped<Abstractions.Auth.ITokenService, Features.Auth.TokenService>();
            return services;
        }
    }
}
