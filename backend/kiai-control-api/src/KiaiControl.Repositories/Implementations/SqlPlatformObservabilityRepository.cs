using System.Text.Json;
using Dapper;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Platform;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlPlatformObservabilityRepository(IDbConnectionFactory factory) : IPlatformObservabilityRepository
{
    public async Task<PlatformDashboard> DashboardAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformObservabilityQueries.Dashboard, new
        {
            filter.OrganizationId, From = filter.From.ToUniversalTime(), To = filter.To.ToUniversalTime(),
            Offset = (filter.Page - 1) * filter.PageSize, filter.PageSize, PlatformOrganizationId = PlatformIdentity.OrganizationId
        }, cancellationToken: ct));
        var summary = await result.ReadSingleAsync<PlatformMetricSummary>();
        var days = (await result.ReadAsync<PlatformMetricDay>()).ToArray();
        var academies = (await result.ReadAsync<PlatformAcademyControl>()).ToArray();
        var total = await result.ReadSingleAsync<int>();
        var routes = (await result.ReadAsync<PlatformRouteMetric>()).ToArray();
        return new(summary, days, new(academies, total), routes);
    }

    public async Task SaveMetricsAsync(IReadOnlyList<PlatformRequestMetric> metrics, CancellationToken ct)
    {
        if (metrics.Count == 0) return;
        using var connection = await factory.OpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(new CommandDefinition(PlatformObservabilityQueries.UpsertMetric, metrics, transaction, cancellationToken: ct));
        transaction.Commit();
    }

    public async Task<PlatformRetentionResult> ApplyRetentionAsync(DateTimeOffset metricsBefore, DateTimeOffset incidentsBefore, DateTimeOffset auditBefore, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();
        await connection.QueryAsync<Guid>(new CommandDefinition(PlatformObservabilityQueries.LockRetentionOrganizations, transaction: transaction, cancellationToken: ct));
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformObservabilityQueries.Retention,
            new { MetricsBefore = metricsBefore, IncidentsBefore = incidentsBefore, AuditBefore = auditBefore }, transaction, cancellationToken: ct));
        var counts = new PlatformRetentionResult(await result.ReadSingleAsync<int>(), await result.ReadSingleAsync<int>(), await result.ReadSingleAsync<int>());
        result.Dispose();
        if (counts.MetricsDeleted + counts.IncidentsDeleted + counts.AuditIdentitiesRedacted > 0)
            await connection.ExecuteAsync(new CommandDefinition(PlatformObservabilityQueries.RecordRetention,
                new { Id = Guid.NewGuid(), PlatformOrganizationId = PlatformIdentity.OrganizationId, Metadata = JsonSerializer.Serialize(counts) }, transaction, cancellationToken: ct));
        transaction.Commit();
        return counts;
    }
}
