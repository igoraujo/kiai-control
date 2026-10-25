using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IProgressionRepository
{
    Task<GraduationSystem?> CreateGraduationSystemAsync(GraduationSystem system, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationSystem>> ListGraduationSystemsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<GraduationLevel?> CreateGraduationLevelAsync(Guid organizationId, GraduationLevel level, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationLevel>> ListGraduationLevelsAsync(Guid organizationId, Guid systemId, CancellationToken cancellationToken = default);

    Task<ClientGraduation?> SetClientGraduationAsync(ClientGraduation graduation, CancellationToken cancellationToken = default);

    Task<GraduationEvent?> PromoteClientAsync(GraduationEvent graduationEvent, bool manualEvaluationPassed, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationEvent>> ListClientGraduationEventsAsync(Guid organizationId, Guid clientId, Guid modalityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EligibleClientResponseRow>> ListEligibleClientsAsync(Guid organizationId, Guid systemId, CancellationToken cancellationToken = default);

    Task<FitnessGoal?> CreateFitnessGoalAsync(FitnessGoal goal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FitnessGoal>> ListFitnessGoalsAsync(Guid organizationId, Guid? clientId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<FitnessProgressRecord?> RecordFitnessProgressAsync(Guid organizationId, FitnessProgressRecord progress, CancellationToken cancellationToken = default);

    Task<bool> CancelFitnessGoalAsync(Guid organizationId, Guid goalId, CancellationToken cancellationToken = default);
}

public sealed class EligibleClientResponseRow
{
    public Guid ClientId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public Guid CurrentLevelId { get; init; }

    public Guid NextLevelId { get; init; }

    public int AttendanceCount { get; init; }

    public decimal AttendancePercentage { get; init; }

    public int TrainingDays { get; init; }
}