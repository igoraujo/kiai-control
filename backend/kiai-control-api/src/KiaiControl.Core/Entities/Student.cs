namespace KiaiControl.Core.Entities;

public sealed class Student
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Status { get; init; } = "active";

    public DateTimeOffset CreatedAt { get; init; }
}
