using KiaiControl.Core.Interfaces;
using KiaiControl.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace KiaiControl.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiServices(
        this IServiceCollection services,
        string redisConnectionString,
        string issuer,
        string audience,
        string signingKey,
        int accessTokenExpirationMinutes,
        int refreshTokenExpirationDays)
    {
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

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
