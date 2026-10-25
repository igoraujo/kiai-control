namespace KiaiControl.Core.Entities;

public sealed class FitnessProgressRecord
{
    public Guid Id { get; set; }

    public Guid FitnessGoalId { get; set; }

    public decimal RecordedValue { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public Guid? CreatedByTeacherId { get; set; }
}