namespace KiaiControl.Core.Entities;

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid LessonId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public DateTimeOffset AttendanceDate { get; set; }

    public string Status { get; set; } = "present";

    public DateTimeOffset? ValidatedAt { get; set; }

    public Guid? ValidatedByPersonId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
