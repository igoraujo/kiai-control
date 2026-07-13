using System.Text;
using KiaiControl.Api.Configuration;
using KiaiControl.Core.Common;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.UseCases.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiApi(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        var connectionStrings = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var useRedisCache = string.Equals(cacheOptions.Provider, "Redis", StringComparison.OrdinalIgnoreCase);

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddProblemDetails();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddAuthorization();
        services.AddScoped<OrganizationContext>();

        var signingKey = string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
            ? "development-signing-key-change-before-production-123456"
            : jwtOptions.SigningKey;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        if (useRedisCache)
        {
            if (string.IsNullOrWhiteSpace(redisOptions.Redis))
            {
                throw new InvalidOperationException("A connection string Redis deve ser configurada quando o provider de cache for Redis.");
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.Redis;
                options.InstanceName = cacheOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHealthChecks();
        services.AddKiaiUseCases();
        services.AddKiaiRepositories(connectionStrings.PostgreSql);
        services.AddKiaiServices(
            redisOptions.Redis,
            useRedisCache,
            jwtOptions.Issuer,
            jwtOptions.Audience,
            signingKey,
            jwtOptions.AccessTokenExpirationMinutes,
            jwtOptions.RefreshTokenExpirationDays);

        return services;
    }
}
