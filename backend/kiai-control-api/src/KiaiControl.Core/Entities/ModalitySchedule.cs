namespace KiaiControl.Core.Entities;

public sealed class ModalitySchedule
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ModalityId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? TeacherId { get; set; }

    public string? BranchName { get; set; }

    public string? TeacherName { get; set; }

    public int DayOfWeek { get; set; }

    public TimeSpan StartsAt { get; set; }

    public TimeSpan EndsAt { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
