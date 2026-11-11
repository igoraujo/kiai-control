using Dapper;
using KiaiControl.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KiaiControl.Api.Services;

public sealed class PlatformDatabaseHealthCheck(IDbConnectionFactory factory) : IHealthCheck
{
    private const string ProbeSql = "select 1;";
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var connection = await factory.OpenConnectionAsync(timeout.Token);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition(ProbeSql, commandTimeout: 3, cancellationToken: timeout.Token));
            return HealthCheckResult.Healthy();
        }
        catch (Exception) { return HealthCheckResult.Unhealthy("Dependência indisponível."); }
    }
}

public sealed class PlatformCacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            await cache.GetAsync("kiai:platform:health-probe", timeout.Token).WaitAsync(timeout.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) { return HealthCheckResult.Unhealthy("Dependência indisponível."); }
    }
}
