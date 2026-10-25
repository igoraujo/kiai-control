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
            throw new InvalidOperationException("A connection string PostgreSql deve ser configurada para registrar os repositórios de producao.");
        }

        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlDbConnectionFactory(connectionString));
        services.AddScoped<IAuthRepository, SqlAuthRepository>();
        services.AddScoped<IClientRepository, SqlClientRepository>();
        services.AddScoped<ITeacherRepository, SqlTeacherRepository>();
        services.AddScoped<IModalityRepository, SqlModalityRepository>();
        services.AddScoped<IClassGroupRepository, SqlClassGroupRepository>();
        services.AddScoped<ILessonRepository, SqlLessonRepository>();
        services.AddScoped<IAttendanceRepository, SqlAttendanceRepository>();
        services.AddScoped<IBillingRepository, SqlBillingRepository>();
        services.AddScoped<IBillingCatalogRepository, SqlBillingCatalogRepository>();
        services.AddScoped<IOrganizationRepository, SqlOrganizationRepository>();
        services.AddScoped<IBranchRepository, SqlBranchRepository>();
        services.AddScoped<INotificationRepository, SqlNotificationRepository>();
        services.AddScoped<IAuditLogRepository, SqlAuditLogRepository>();
        services.AddScoped<IRankingRepository, SqlRankingRepository>();
        services.AddScoped<IAccessControlRepository, SqlAccessControlRepository>();
        services.AddScoped<IReportRepository, SqlReportRepository>();
        services.AddScoped<IProgressionRepository, SqlProgressionRepository>();

        return services;
    }
}
