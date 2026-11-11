using System.ComponentModel.DataAnnotations;
using KiaiControl.Contracts.Auth;

namespace KiaiControl.Contracts.Platform;

public sealed class PlatformArchiveRequest
{
    [Required, StringLength(160)] public string ConfirmName { get; set; } = "";
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
    public bool Reactivate { get; set; }
}

public sealed class PlatformPrivacyHoldRequest
{
    public bool Hold { get; set; }
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class PlatformAuditDetailsRequest
{
    public Guid OrganizationId { get; set; }
}

public sealed class PlatformOrganizationUpdateRequest
{
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [StringLength(160)] public string? TradeName { get; set; }
    [StringLength(30)] public string? Document { get; set; }
    [EmailAddress, StringLength(160)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class OrganizationStatusRequest
{
    [Required] public string Status { get; set; } = "";
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class CreatePlatformAccountRequest
{
    [Required, StringLength(180)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    [Required, ValidPassword] public string TemporaryPassword { get; set; } = "";
    [Required] public string Role { get; set; } = "owner";
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class UpdatePlatformAccountRequest
{
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
    [Required] public string[] Roles { get; set; } = [];
    public bool IsActive { get; set; }
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class PlatformPasswordRequest
{
    [Required, ValidPassword] public string TemporaryPassword { get; set; } = "";
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class PlatformReasonRequest
{
    [Required, StringLength(500, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class PlatformAccountResponse
{
    public Guid UserId { get; init; }
    public Guid PersonId { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public bool IsActive { get; init; }
    public bool MustChangePassword { get; init; }
    public string[] Roles { get; init; } = [];
}

public sealed class PlatformAuditResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = "";
    public Guid? ActorUserId { get; init; }
    public string? ActorName { get; init; }
    public string? ActorEmail { get; init; }
    public string Action { get; init; } = "";
    public string EntityName { get; init; } = "";
    public Guid? EntityId { get; init; }
    public string? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PlatformIncidentResponse
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? OrganizationName { get; init; }
    public Guid? ActorUserId { get; init; }
    public string Category { get; init; } = "";
    public string Method { get; init; } = "";
    public string Route { get; init; } = "";
    public int StatusCode { get; init; }
    public string CorrelationId { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PlatformOverviewResponse
{
    public int Organizations { get; init; }
    public int ActiveOrganizations { get; init; }
    public int SuspendedOrganizations { get; init; }
    public int RequestFailures { get; init; }
    public int AuthenticationFailures { get; init; }
    public int AuditFailures { get; init; }
}
