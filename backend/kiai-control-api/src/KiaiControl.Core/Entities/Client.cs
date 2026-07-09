namespace KiaiControl.Core.Entities;

public sealed class Client
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
