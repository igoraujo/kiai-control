namespace KiaiControl.Core.Entities;

public sealed class ClassSchedule
{
    public Guid Id { get; set; }

    public Guid ClassGroupId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}