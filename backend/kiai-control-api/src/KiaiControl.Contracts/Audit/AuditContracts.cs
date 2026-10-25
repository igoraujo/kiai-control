namespace KiaiControl.Contracts.Audit;

public sealed class AuditLogResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid? ActorPersonId { get; init; }

    public Guid? ActorUserAccountId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityName { get; init; } = string.Empty;

    public Guid? EntityId { get; init; }

    public string? Metadata { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}