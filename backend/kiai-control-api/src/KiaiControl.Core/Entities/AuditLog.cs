namespace KiaiControl.Core.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? ActorPersonId { get; set; }

    public Guid? ActorUserAccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}