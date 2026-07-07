using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KiaiControl.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiServices(this IServiceCollection services, string redisConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        return services;
    }
}
