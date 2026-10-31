namespace KiaiControl.Contracts.Lessons;

public sealed class CreateLessonRequest
{
    public Guid ModalityId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }
}

public sealed class UpdateLessonRequest
{
    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }
}

public sealed class CancelLessonRequest
{
    public string? Reason { get; set; }
}

public sealed class RescheduleLessonRequest
{
    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }
}

public sealed class LessonResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ModalityId { get; init; }

    public Guid? ModalityScheduleId { get; init; }

    public string? ModalityName { get; init; }

    public string? BranchName { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset EndsAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? CancellationReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}