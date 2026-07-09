using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Connections;
using KiaiControl.Repositories.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace KiaiControl.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiRepositories(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fallback seguro para ambientes sem banco configurado.
            services.AddScoped<IClientRepository, InMemoryClientRepository>();
            services.AddScoped<ITeacherRepository, InMemoryTeacherRepository>();
            services.AddScoped<IAttendanceRepository, InMemoryAttendanceRepository>();
            services.AddScoped<IBillingRepository, InMemoryBillingRepository>();
            return services;
        }

        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlDbConnectionFactory(connectionString));
        services.AddScoped<IClientRepository, SqlClientRepository>();
        services.AddScoped<ITeacherRepository, SqlTeacherRepository>();
        services.AddScoped<IAttendanceRepository, SqlAttendanceRepository>();
        services.AddScoped<IBillingRepository, SqlBillingRepository>();

        return services;
    }
}
