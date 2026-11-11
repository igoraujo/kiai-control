namespace KiaiControl.Contracts.Platform;

public sealed class PlatformMetricsResponse
{
    public long Requests { get; init; }
    public long ServerErrors { get; init; }
    public long ClientErrors { get; init; }
    public double AverageDurationMs { get; init; }
    public double MaxDurationMs { get; init; }
    public int Active { get; init; }
    public int Suspended { get; init; }
    public int Inactive { get; init; }
    public int Archived { get; init; }
    public int PrivacyHeld { get; init; }
    public int WithoutActiveOwner { get; init; }
    public IReadOnlyList<PlatformMetricDayResponse> Days { get; init; } = [];
    public IReadOnlyList<PlatformAcademyControlResponse> Academies { get; init; } = [];
    public int TotalAcademies { get; init; }
    public IReadOnlyList<PlatformRouteMetricResponse> Routes { get; init; } = [];
}

public sealed record PlatformMetricDayResponse(DateTimeOffset Day, long Requests, long ServerErrors, double AverageDurationMs);
public sealed record PlatformAcademyControlResponse(Guid Id, string Name, string Status, bool Archived, bool PrivacyHold,
    int Accounts, int ActiveOwners, int BlockedAccounts, int PendingPasswords, long Requests, long ServerErrors, int Incidents, DateTimeOffset? LastActivityAt);
public sealed record PlatformRouteMetricResponse(string Route, string Method, long Requests, long ServerErrors, double AverageDurationMs);
public sealed record PlatformDependencyResponse(string Name, string Status, double DurationMs);
public sealed record PlatformHealthResponse(string Status, DateTimeOffset CheckedAt, IReadOnlyList<PlatformDependencyResponse> Dependencies);
public sealed record PlatformRetentionPolicyResponse(int MetricsDays, int IncidentsDays, int AuditIdentityDays, string Timezone);
