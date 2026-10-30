namespace KiaiControl.Core.Entities;

public sealed class Enrollment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClassGroupId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public DateTimeOffset EnrolledAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string Status { get; set; } = "active";
}