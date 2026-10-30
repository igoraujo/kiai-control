using System.Text;
using KiaiControl.Contracts.Auth;
using KiaiControl.Api.Configuration;
using KiaiControl.Core.Common;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.UseCases.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
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
        services.AddAuthorization(options =>
        {
            foreach (var permissionCode in PermissionCodes.All)
            {
                options.AddPolicy(permissionCode, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(PermissionCodes.ClaimType, permissionCode));
            }
        });
        services.AddScoped<OrganizationContext>();

        jwtOptions.Validate();
        var signingKey = jwtOptions.ResolveSigningKey(environment.IsDevelopment());

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    ClockSkew = TimeSpan.FromSeconds(30)
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
