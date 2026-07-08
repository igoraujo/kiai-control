namespace KiaiControl.Core.Entities;

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid StudentId { get; set; }

    public DateTimeOffset AttendanceDate { get; set; }

    public string Status { get; set; } = "present";

    public DateTimeOffset CreatedAt { get; set; }
}
