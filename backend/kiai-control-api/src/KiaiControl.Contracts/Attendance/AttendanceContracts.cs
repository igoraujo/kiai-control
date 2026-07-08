namespace KiaiControl.Contracts.Attendance;

public sealed class CreateAttendanceRequest
{
    public Guid OrganizationId { get; set; }

    public Guid StudentId { get; set; }

    public DateTimeOffset AttendanceDate { get; set; }

    public string Status { get; set; } = "present";
}

public sealed class AttendanceRecordResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid StudentId { get; init; }

    public DateTimeOffset AttendanceDate { get; init; }

    public string Status { get; init; } = string.Empty;
}
