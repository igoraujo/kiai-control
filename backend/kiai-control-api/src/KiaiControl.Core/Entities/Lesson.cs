namespace KiaiControl.Core.Entities;

public sealed class Lesson
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ModalityId { get; set; }

    public Guid? ModalityScheduleId { get; set; }

    public string? ModalityName { get; set; }

    public string? BranchName { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public string Status { get; set; } = "scheduled";

    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}