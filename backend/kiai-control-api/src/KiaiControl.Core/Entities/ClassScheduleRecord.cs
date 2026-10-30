namespace KiaiControl.Core.Entities;

public sealed class ClassScheduleRecord
{
    public Guid Id { get; set; }

    public Guid ClassGroupId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeSpan StartsAt { get; set; }

    public TimeSpan EndsAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}