namespace KiaiControl.Core.Entities;

public sealed class FitnessGoal
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClientId { get; set; }

    public Guid? ModalityId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string GoalType { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }

    public decimal CurrentValue { get; set; }

    public string? Unit { get; set; }

    public DateOnly StartsAt { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}