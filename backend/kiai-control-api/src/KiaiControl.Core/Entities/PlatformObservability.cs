namespace KiaiControl.Core.Entities;

public sealed class PlatformRequestMetric
{
    public DateTimeOffset BucketStart { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Route { get; set; } = "";
    public string Method { get; set; } = "";
    public int StatusCode { get; set; }
    public long Requests { get; set; }
    public double DurationTotalMs { get; set; }
    public double MaxDurationMs { get; set; }
}

public sealed class PlatformMetricSummary
{
    public long Requests { get; set; }
    public long ServerErrors { get; set; }
    public long ClientErrors { get; set; }
    public double AverageDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public int Active { get; set; }
    public int Suspended { get; set; }
    public int Inactive { get; set; }
    public int Archived { get; set; }
    public int PrivacyHeld { get; set; }
    public int WithoutActiveOwner { get; set; }
}

public sealed class PlatformMetricDay
{
    public DateTimeOffset Day { get; set; }
    public long Requests { get; set; }
    public long ServerErrors { get; set; }
    public double AverageDurationMs { get; set; }
}

public sealed class PlatformAcademyControl
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public bool Archived { get; set; }
    public bool PrivacyHold { get; set; }
    public int Accounts { get; set; }
    public int ActiveOwners { get; set; }
    public int BlockedAccounts { get; set; }
    public int PendingPasswords { get; set; }
    public long Requests { get; set; }
    public long ServerErrors { get; set; }
    public int Incidents { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
}

public sealed class PlatformRouteMetric
{
    public string Route { get; set; } = "";
    public string Method { get; set; } = "";
    public long Requests { get; set; }
    public long ServerErrors { get; set; }
    public double AverageDurationMs { get; set; }
}

public sealed record PlatformDashboard(PlatformMetricSummary Summary, IReadOnlyList<PlatformMetricDay> Days,
    PagedResult<PlatformAcademyControl> Academies, IReadOnlyList<PlatformRouteMetric> Routes);

public sealed record PlatformRetentionResult(int MetricsDeleted, int IncidentsDeleted, int AuditIdentitiesRedacted);
