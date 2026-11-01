namespace KiaiControl.Core.Entities;

public sealed class GraduationBelt
{
    public Guid Id { get; set; }

    public Guid GraduationSystemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int HierarchyOrder { get; set; }

    public int DegreeCount { get; set; }

    public int MinLessons { get; set; }

    public int MinTrainingDays { get; set; }

    public decimal? MinAttendancePercentage { get; set; }

    public bool RequiresManualEvaluation { get; set; }
}
