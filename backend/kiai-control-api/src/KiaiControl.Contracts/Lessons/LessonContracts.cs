namespace KiaiControl.Contracts.Lessons;

public sealed class CreateLessonRequest
{
    public Guid ClassGroupId { get; set; }

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

public sealed class GenerateLessonsRequest
{
    public Guid ClassGroupId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}

public sealed class LessonResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClassGroupId { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset EndsAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? CancellationReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}