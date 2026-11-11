namespace KiaiControl.Core.Entities;

public sealed class PlatformAccount
{
    public Guid UserId { get; set; }
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public string[] Roles { get; set; } = [];
}

public sealed class PlatformIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Category { get; set; } = "request_failure";
    public string Method { get; set; } = "";
    public string Route { get; set; } = "";
    public int StatusCode { get; set; }
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PlatformOverview
{
    public int Organizations { get; set; }
    public int ActiveOrganizations { get; set; }
    public int SuspendedOrganizations { get; set; }
    public int RequestFailures { get; set; }
    public int AuthenticationFailures { get; set; }
    public int AuditFailures { get; set; }
}

public sealed record PlatformLogFilter(
    Guid? OrganizationId, Guid? ActorUserId, string? Action,
    DateTimeOffset From, DateTimeOffset To, int Page, int PageSize, string? ActorSearch = null);
