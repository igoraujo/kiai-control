using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IPlatformObservabilityRepository
{
    Task<PlatformDashboard> DashboardAsync(PlatformLogFilter filter, CancellationToken ct);
    Task SaveMetricsAsync(IReadOnlyList<PlatformRequestMetric> metrics, CancellationToken ct);
    Task<PlatformRetentionResult> ApplyRetentionAsync(DateTimeOffset metricsBefore, DateTimeOffset incidentsBefore, DateTimeOffset auditBefore, CancellationToken ct);
}
