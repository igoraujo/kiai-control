namespace KiaiControl.Contracts.Teachers;

public sealed class TeacherPortalProfileResponse
{
    public Guid TeacherId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? PreferredName { get; init; }
}

public sealed class TeacherPortalLessonResponse
{
    public Guid Id { get; init; }

    public string? ModalityName { get; init; }

    public string? BranchName { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset EndsAt { get; init; }

    public string Status { get; init; } = string.Empty;
}
