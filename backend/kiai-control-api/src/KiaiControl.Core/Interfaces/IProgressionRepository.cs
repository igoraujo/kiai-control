using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IProgressionRepository
{
    Task<GraduationSystem?> CreateGraduationSystemAsync(GraduationSystem system, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationSystem>> ListGraduationSystemsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationBelt>> ListGraduationBeltsAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken = default);

    Task<GraduationBelt?> CreateGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        GraduationBelt belt,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        GraduationBelt belt,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        CancellationToken cancellationToken = default);

    Task<GraduationLevel?> CreateGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        GraduationLevel degree,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        GraduationLevel degree,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        CancellationToken cancellationToken = default);

    Task<GraduationLevel?> CreateGraduationLevelAsync(Guid organizationId, GraduationLevel level, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationLevel>> ListGraduationLevelsAsync(Guid organizationId, Guid systemId, CancellationToken cancellationToken = default);

    Task<ClientGraduation?> SetClientGraduationAsync(ClientGraduation graduation, CancellationToken cancellationToken = default);

    Task<GraduationEvent?> PromoteClientAsync(GraduationEvent graduationEvent, bool manualEvaluationPassed, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationEvent>> ListClientGraduationEventsAsync(Guid organizationId, Guid clientId, Guid modalityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EligibleClientResponseRow>> ListEligibleClientsAsync(Guid organizationId, Guid systemId, CancellationToken cancellationToken = default);

    Task<FitnessGoal?> CreateFitnessGoalAsync(FitnessGoal goal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FitnessGoal>> ListFitnessGoalsAsync(Guid organizationId, Guid? clientId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<FitnessProgressRecord?> RecordFitnessProgressAsync(Guid organizationId, FitnessProgressRecord progress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FitnessProgressRecord>> ListFitnessProgressRecordsAsync(
        Guid organizationId,
        Guid goalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

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