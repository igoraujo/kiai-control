using KiaiControl.Core.Interfaces;
using KiaiControl.Services.Cache;
using KiaiControl.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KiaiControl.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiServices(
        this IServiceCollection services,
        string redisConnectionString,
        bool useRedisCache,
        string issuer,
        string audience,
        string signingKey,
        int accessTokenExpirationMinutes,
        int refreshTokenExpirationDays)
    {
        if (useRedisCache && !string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        services.AddSingleton<DistributedCacheManager>();
        services.AddSingleton<ICacheAspect, DistributedCacheAspect>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAuthTokenService>(_ => new JwtAuthTokenService(
            issuer,
            audience,
            signingKey,
            accessTokenExpirationMinutes,
            refreshTokenExpirationDays));

        return services;
    }
}
