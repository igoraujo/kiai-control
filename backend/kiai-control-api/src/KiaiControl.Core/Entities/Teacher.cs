namespace KiaiControl.Core.Entities;

public sealed class Teacher
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; }
}
