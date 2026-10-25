namespace KiaiControl.Contracts.Graduation;

public sealed class CreateGraduationSystemRequest
{
    public Guid ModalityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed class GraduationSystemResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ModalityId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateGraduationLevelRequest
{
    public string Name { get; set; } = string.Empty;

    public int HierarchyOrder { get; set; }

    public int MinTrainingDays { get; set; }

    public decimal? MinAttendancePercentage { get; set; }

    public int MinLessons { get; set; }

    public int? MinAge { get; set; }

    public bool RequiresManualEvaluation { get; set; }
}

public sealed class GraduationLevelResponse
{
    public Guid Id { get; init; }

    public Guid GraduationSystemId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int HierarchyOrder { get; init; }

    public int MinTrainingDays { get; init; }

    public decimal? MinAttendancePercentage { get; init; }

    public int MinLessons { get; init; }

    public int? MinAge { get; init; }

    public bool RequiresManualEvaluation { get; init; }
}

public sealed class SetClientGraduationRequest
{
    public Guid ModalityId { get; set; }

    public Guid LevelId { get; set; }

    public DateOnly GrantedAt { get; set; }

    public Guid? TeacherId { get; set; }

    public string? Notes { get; set; }
}

public sealed class PromoteClientRequest
{
    public Guid ModalityId { get; set; }

    public Guid ToLevelId { get; set; }

    public DateOnly EventDate { get; set; }

    public Guid? TeacherId { get; set; }

    public string? Justification { get; set; }

    public bool ManualEvaluationPassed { get; set; }
}

public sealed class ClientGraduationResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientId { get; init; }

    public Guid ModalityId { get; init; }

    public Guid CurrentLevelId { get; init; }

    public DateOnly GrantedAt { get; init; }

    public Guid? GrantedByTeacherId { get; init; }

    public string? Notes { get; init; }
}

public sealed class GraduationEventResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientId { get; init; }

    public Guid ModalityId { get; init; }

    public Guid? FromLevelId { get; init; }

    public Guid ToLevelId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public DateOnly EventDate { get; init; }

    public Guid? TeacherId { get; init; }

    public string? Justification { get; init; }
}

public sealed class EligibleClientResponse
{
    public Guid ClientId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public Guid CurrentLevelId { get; init; }

    public Guid NextLevelId { get; init; }

    public int AttendanceCount { get; init; }

    public decimal AttendancePercentage { get; init; }

    public int TrainingDays { get; init; }
}