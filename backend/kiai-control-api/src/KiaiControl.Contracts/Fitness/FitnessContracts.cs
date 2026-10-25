namespace KiaiControl.Contracts.Fitness;

public sealed class CreateFitnessGoalRequest
{
    public Guid ClientId { get; set; }

    public Guid? ModalityId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string GoalType { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }

    public string? Unit { get; set; }

    public DateOnly StartsAt { get; set; }

    public DateOnly DueDate { get; set; }
}

public sealed class FitnessGoalResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientId { get; init; }

    public Guid? ModalityId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string GoalType { get; init; } = string.Empty;

    public decimal TargetValue { get; init; }

    public decimal CurrentValue { get; init; }

    public string? Unit { get; init; }

    public DateOnly StartsAt { get; init; }

    public DateOnly DueDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class RecordFitnessProgressRequest
{
    public decimal RecordedValue { get; set; }

    public string? Notes { get; set; }
}

public sealed class FitnessProgressResponse
{
    public Guid Id { get; init; }

    public Guid FitnessGoalId { get; init; }

    public decimal RecordedValue { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public Guid? CreatedByTeacherId { get; init; }
}

public sealed class FitnessSummaryResponse
{
    public Guid ClientId { get; init; }

    public int ActiveGoals { get; init; }

    public int AchievedGoals { get; init; }

    public IReadOnlyList<FitnessGoalResponse> Goals { get; init; } = [];
}