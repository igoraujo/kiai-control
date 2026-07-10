using System.Text;
using KiaiControl.Api.Configuration;
using KiaiControl.Core.Common;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.UseCases.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiApi(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

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

        services.AddHealthChecks();
        services.AddKiaiUseCases();
        services.AddKiaiRepositories(connectionStrings.PostgreSql);
        services.AddKiaiServices(
            redisOptions.Redis,
            jwtOptions.Issuer,
            jwtOptions.Audience,
            signingKey,
            jwtOptions.AccessTokenExpirationMinutes,
            jwtOptions.RefreshTokenExpirationDays);

        return services;
    }
}
