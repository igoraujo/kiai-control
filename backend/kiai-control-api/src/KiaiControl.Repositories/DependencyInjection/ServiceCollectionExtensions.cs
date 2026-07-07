using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace KiaiControl.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiRepositories(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlDbConnectionFactory(connectionString));
        return services;
    }
}
