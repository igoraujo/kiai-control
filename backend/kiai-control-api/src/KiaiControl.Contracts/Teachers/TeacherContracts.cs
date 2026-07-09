namespace KiaiControl.Contracts.Teachers;

public sealed class CreateTeacherRequest
{
    public Guid OrganizationId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = "active";
}

public sealed class UpdateTeacherRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = "active";
}

public sealed class TeacherResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
