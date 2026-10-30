namespace KiaiControl.Core.Entities;

public sealed class ClassGroupTeacherAssignment
{
    public Guid Id { get; set; }

    public Guid ClassGroupId { get; set; }

    public Guid TeacherId { get; set; }

    public string TeacherName { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }
}