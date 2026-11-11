using System.Text;
using System.Threading.RateLimiting;
using KiaiControl.Contracts.Auth;
using KiaiControl.Api.Configuration;
using KiaiControl.Api.Services;
using KiaiControl.Core.Common;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.UseCases.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.Api.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        var connectionStrings = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var useRedisCache = string.Equals(cacheOptions.Provider, "Redis", StringComparison.OrdinalIgnoreCase);
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PlatformOwnerOptions>(configuration.GetSection(PlatformOwnerOptions.SectionName));
        services.AddKiaiEmail(configuration, environment.IsDevelopment());
        services.AddHttpContextAccessor();
        services.AddProblemDetails();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddAuthorization(options =>
        {
            foreach (var permissionCode in PermissionCodes.All)
            {
                options.AddPolicy(permissionCode, policy => policy.RequireAuthenticatedUser().RequireAssertion(context => context.User.HasClaim(PermissionCodes.ClaimType, permissionCode) || (PermissionCodes.ReadPermissions.TryGetValue(permissionCode, out var readPermission) && context.Resource is HttpContext http && HttpMethods.IsGet(http.Request.Method) && context.User.HasClaim(PermissionCodes.ClaimType, readPermission))));
            }
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = Math.Clamp(configuration.GetValue("RateLimiting:AuthPermitLimit", 10), 1, 1000), Window = TimeSpan.FromMinutes(1), QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst }));
        });
        services.AddScoped<OrganizationContext>();
        services.AddScoped<PlatformOwnerBootstrapper>();
        services.AddScoped<PlatformAccessAudit>();
        services.AddScoped<PlatformIncidentRecorder>();
        services.AddOptions<PlatformPrivacyOptions>().Bind(configuration.GetSection("PlatformPrivacy"))
            .Validate(policy => policy.IsValid(), "Os prazos de retenção devem estar entre 1 e 3650 dias; flush entre 1 e 300 segundos.")
            .ValidateOnStart();
        services.AddSingleton<PlatformTelemetryBuffer>();
        services.AddHostedService<PlatformTelemetryWorker>();
        jwtOptions.Validate();
        var signingKey = jwtOptions.ResolveSigningKey(environment.IsDevelopment());
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    var version = context.Principal?.FindFirst("session_version")?.Value;
                    if (!Guid.TryParse(userId, out var id) || !int.TryParse(version, out var sessionVersion))
                    {
                        context.Fail("Sessão inválida.");
                        return;
                    }

                    var repository = context.HttpContext.RequestServices.GetRequiredService<KiaiControl.Core.Interfaces.IAuthRepository>();
                    var user = await repository.GetByIdAsync(id, context.HttpContext.RequestAborted);
                    if (user is null || !user.IsActive || user.SessionVersion != sessionVersion)
                    {
                        context.Fail("Sessão encerrada.");
                        return;
                    }

                    var roles = context.Principal!.FindAll("role").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var permissions = context.Principal.FindAll("permission").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (!roles.SetEquals(user.Roles) || !permissions.SetEquals(user.Permissions))
                        context.Fail("Permissões atualizadas. Renove a sessão.");
                }
            };
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
                throw new InvalidOperationException(SystemMessages.AConnectionStringRedisDeveSerConfiguradaQuandoOProviderDeCacheForRedis);
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

        services.AddHealthChecks()
            .AddCheck<PlatformDatabaseHealthCheck>("PostgreSQL", tags: ["ready"], timeout: TimeSpan.FromSeconds(4))
            .AddCheck<PlatformCacheHealthCheck>(useRedisCache ? "Redis" : "Cache em memória", tags: ["ready"], timeout: TimeSpan.FromSeconds(4));
        services.AddKiaiUseCases();
        services.AddKiaiRepositories(connectionStrings.PostgreSql);
        services.AddKiaiServices(redisOptions.Redis, useRedisCache, jwtOptions.Issuer, jwtOptions.Audience, signingKey, jwtOptions.AccessTokenExpirationMinutes, jwtOptions.RefreshTokenExpirationDays);
        return services;
    }
}
