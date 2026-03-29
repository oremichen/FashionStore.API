using System;
using Microsoft.Extensions.DependencyInjection;

namespace FashionStore.Infrastructure
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
           return services;
        }
    }
}
