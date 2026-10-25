using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Progression;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlProgressionRepository(IDbConnectionFactory dbConnectionFactory) : IProgressionRepository
{
    public async Task<GraduationSystem?> CreateGraduationSystemAsync(GraduationSystem system, CancellationToken cancellationToken = default)
    {
        system.Id = Guid.NewGuid();
        system.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<GraduationSystem>(ProgressionQueries.CreateGraduationSystem, system);
    }

    public async Task<IReadOnlyList<GraduationSystem>> ListGraduationSystemsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<GraduationSystem>(ProgressionQueries.ListGraduationSystems, new { OrganizationId = organizationId });
        return items.ToList();
    }

    public async Task<GraduationLevel?> CreateGraduationLevelAsync(Guid organizationId, GraduationLevel level, CancellationToken cancellationToken = default)
    {
        level.Id = Guid.NewGuid();
        level.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<GraduationLevel>(ProgressionQueries.CreateGraduationLevel, new
        {
            OrganizationId = organizationId,
            level.Id,
            level.GraduationSystemId,
            level.Name,
            level.HierarchyOrder,
            level.MinTrainingDays,
            level.MinAttendancePercentage,
            level.MinLessons,
            level.MinAge,
            level.RequiresManualEvaluation,
            level.CreatedAt
        });
    }

    public async Task<IReadOnlyList<GraduationLevel>> ListGraduationLevelsAsync(Guid organizationId, Guid systemId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<GraduationLevel>(ProgressionQueries.ListGraduationLevels, new { OrganizationId = organizationId, SystemId = systemId });
        return items.ToList();
    }

    public async Task<ClientGraduation?> SetClientGraduationAsync(ClientGraduation graduation, CancellationToken cancellationToken = default)
    {
        graduation.Id = Guid.NewGuid();
        graduation.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClientGraduation>(ProgressionQueries.SetClientGraduation, new
        {
            graduation.Id,
            graduation.OrganizationId,
            graduation.ClientId,
            graduation.ModalityId,
            CurrentLevelId = graduation.CurrentLevelId,
            graduation.GrantedAt,
            graduation.GrantedByTeacherId,
            graduation.Notes,
            graduation.CreatedAt
        });
    }

    public async Task<GraduationEvent?> PromoteClientAsync(
        GraduationEvent graduationEvent,
        bool manualEvaluationPassed,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var current = await connection.QuerySingleOrDefaultAsync<GraduationState>(
            ProgressionQueries.LockGraduationForPromotion,
            new
            {
                graduationEvent.OrganizationId,
                graduationEvent.ClientId,
                graduationEvent.ModalityId,
                graduationEvent.ToLevelId
            },
            transaction);
        if (current is null || current.NextOrder <= current.CurrentOrder || (current.RequiresManualEvaluation && !manualEvaluationPassed))
        {
            return null;
        }

        graduationEvent.Id = Guid.NewGuid();
        graduationEvent.FromLevelId = current.CurrentLevelId;
        graduationEvent.EventType = "promotion";
        graduationEvent.CreatedAt = DateTimeOffset.UtcNow;
        var created = await connection.QuerySingleOrDefaultAsync<GraduationEvent>(
            ProgressionQueries.InsertGraduationEvent,
            graduationEvent,
            transaction);
        if (created is null)
        {
            return null;
        }

        await connection.ExecuteAsync(ProgressionQueries.UpdateClientGraduation, new
        {
            graduationEvent.OrganizationId,
            graduationEvent.ClientId,
            graduationEvent.ModalityId,
            graduationEvent.ToLevelId,
            graduationEvent.EventDate,
            graduationEvent.TeacherId,
            graduationEvent.Justification
        }, transaction);
        transaction.Commit();
        return created;
    }

    public async Task<IReadOnlyList<GraduationEvent>> ListClientGraduationEventsAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<GraduationEvent>(ProgressionQueries.ListGraduationEvents, new
        {
            OrganizationId = organizationId,
            ClientId = clientId,
            ModalityId = modalityId
        });
        return items.ToList();
    }

    public async Task<IReadOnlyList<EligibleClientResponseRow>> ListEligibleClientsAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<EligibleClientResponseRow>(ProgressionQueries.ListEligibleClients, new
        {
            OrganizationId = organizationId,
            SystemId = systemId
        });
        return items.ToList();
    }

    public async Task<FitnessGoal?> CreateFitnessGoalAsync(FitnessGoal goal, CancellationToken cancellationToken = default)
    {
        goal.Id = Guid.NewGuid();
        goal.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<FitnessGoal>(ProgressionQueries.CreateFitnessGoal, goal);
    }

    public async Task<IReadOnlyList<FitnessGoal>> ListFitnessGoalsAsync(
        Guid organizationId,
        Guid? clientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<FitnessGoal>(ProgressionQueries.ListFitnessGoals, new
        {
            OrganizationId = organizationId,
            ClientId = clientId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return items.ToList();
    }

    public async Task<FitnessProgressRecord?> RecordFitnessProgressAsync(
        Guid organizationId,
        FitnessProgressRecord progress,
        CancellationToken cancellationToken = default)
    {
        progress.Id = Guid.NewGuid();
        progress.RecordedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var recorded = await connection.QuerySingleOrDefaultAsync<FitnessProgressRecord>(ProgressionQueries.RecordFitnessProgress, new
        {
            progress.Id,
            OrganizationId = organizationId,
            progress.FitnessGoalId,
            progress.RecordedValue,
            progress.Notes,
            progress.RecordedAt,
            progress.CreatedByTeacherId
        }, transaction);
        if (recorded is null)
        {
            return null;
        }

        transaction.Commit();
        return recorded;
    }

    public async Task<bool> CancelFitnessGoalAsync(Guid organizationId, Guid goalId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(ProgressionQueries.CancelFitnessGoal, new { OrganizationId = organizationId, GoalId = goalId });
        return affected > 0;
    }

    private sealed class GraduationState
    {
        public Guid CurrentLevelId { get; init; }

        public Guid ModalityId { get; init; }

        public Guid GraduationSystemId { get; init; }

        public int CurrentOrder { get; init; }

        public int NextOrder { get; init; }

        public bool RequiresManualEvaluation { get; init; }
    }
}