namespace KiaiControl.Contracts.Modalities;

public sealed class CreateModalityScheduleRequest
{
    public Guid? BranchId { get; set; }

    public Guid? TeacherId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }
}

public sealed class ModalityScheduleResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ModalityId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? TeacherId { get; init; }

    public string? BranchName { get; init; }

    public string? TeacherName { get; init; }

    public int DayOfWeek { get; init; }

    public TimeOnly StartsAt { get; init; }

    public TimeOnly EndsAt { get; init; }

    public bool IsActive { get; init; }
}
