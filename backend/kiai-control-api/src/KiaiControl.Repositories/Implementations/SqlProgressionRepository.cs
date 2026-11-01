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

    public async Task<IReadOnlyList<GraduationBelt>> ListGraduationBeltsAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var belts = await connection.QueryAsync<GraduationBelt>(ProgressionQueries.ListGraduationBelts, new
        {
            OrganizationId = organizationId,
            SystemId = systemId
        });
        return belts.ToList();
    }

    public async Task<GraduationBelt?> CreateGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        GraduationBelt belt,
        CancellationToken cancellationToken = default)
    {
        belt.Id = Guid.NewGuid();
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var system = await connection.QuerySingleOrDefaultAsync<GraduationSystemOrder>(
            ProgressionQueries.LockGraduationSystem,
            new { OrganizationId = organizationId, SystemId = systemId, belt.Name },
            transaction);
        if (system is null || system.NameExists || system.CurrentOrder >= (int.MaxValue - 99) / 100)
        {
            return null;
        }

        belt.GraduationSystemId = systemId;
        belt.HierarchyOrder = system.CurrentOrder + 1;
        await connection.ExecuteAsync(ProgressionQueries.InsertGraduationBelt, new
        {
            belt.Id,
            SystemId = systemId,
            belt.Name,
            belt.HierarchyOrder,
            belt.DegreeCount,
            LevelId = Guid.NewGuid(),
            LevelOrder = belt.HierarchyOrder * 100,
            belt.MinLessons,
            belt.MinTrainingDays,
            belt.MinAttendancePercentage,
            belt.RequiresManualEvaluation
        }, transaction);
        var created = await connection.QuerySingleAsync<GraduationBelt>(
            ProgressionQueries.GetGraduationBelt,
            new { BeltId = belt.Id },
            transaction);
        transaction.Commit();
        return created;
    }

    public async Task<bool> UpdateGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        GraduationBelt belt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var current = await connection.QuerySingleOrDefaultAsync<GraduationBeltState>(
            ProgressionQueries.LockGraduationBelt,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = belt.Id },
            transaction);
        if (current is null || belt.DegreeCount < current.CurrentDegree)
        {
            return false;
        }

        var nameExists = await connection.QuerySingleAsync<bool>(
            ProgressionQueries.GraduationBeltNameExists,
            new { SystemId = systemId, BeltId = belt.Id, belt.Name },
            transaction);
        if (nameExists)
        {
            return false;
        }

        var levelNameExists = await connection.QuerySingleAsync<bool>(
            ProgressionQueries.GraduationBeltLevelNameExists,
            new { SystemId = systemId, BeltId = belt.Id, belt.Name },
            transaction);
        if (levelNameExists)
        {
            return false;
        }

        await connection.ExecuteAsync(ProgressionQueries.UpdateGraduationBelt, new
        {
            SystemId = systemId,
            BeltId = belt.Id,
            belt.Name,
            belt.DegreeCount,
            belt.MinLessons,
            belt.MinTrainingDays,
            belt.MinAttendancePercentage,
            belt.RequiresManualEvaluation
        }, transaction);
        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteGraduationBeltAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var belt = await connection.QuerySingleOrDefaultAsync<GraduationBeltState>(
            ProgressionQueries.LockGraduationBelt,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = beltId },
            transaction);
        if (belt is null || await connection.QuerySingleAsync<bool>(
                ProgressionQueries.GraduationBeltHasHistory, new { BeltId = beltId }, transaction))
        {
            return false;
        }

        await connection.ExecuteAsync(ProgressionQueries.DeleteGraduationBeltLevels, new { BeltId = beltId }, transaction);
        var deleted = await connection.QuerySingleOrDefaultAsync<Guid?>(
            ProgressionQueries.DeleteGraduationBelt,
            new { BeltId = beltId, SystemId = systemId },
            transaction);
        if (deleted is null)
        {
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<GraduationLevel?> CreateGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        GraduationLevel degree,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var belt = await connection.QuerySingleOrDefaultAsync<GraduationBeltState>(
            ProgressionQueries.LockGraduationBelt,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = beltId },
            transaction);
        if (belt is null || belt.CurrentDegree >= belt.DegreeCount || belt.HierarchyOrder > (int.MaxValue - 99) / 100)
        {
            return null;
        }

        degree.Id = Guid.NewGuid();
        degree.GraduationSystemId = systemId;
        degree.BeltId = beltId;
        degree.BeltName = belt.Name;
        degree.DegreeNumber = belt.CurrentDegree + 1;
        degree.HierarchyOrder = belt.HierarchyOrder * 100 + degree.DegreeNumber;
        degree.CreatedAt = DateTimeOffset.UtcNow;
        var created = await connection.QuerySingleOrDefaultAsync<GraduationLevel>(
            ProgressionQueries.InsertGraduationDegree,
            new
            {
                OrganizationId = organizationId,
                SystemId = systemId,
                BeltId = beltId,
                degree.Id,
                degree.DegreeNumber,
                degree.MinTrainingDays,
                degree.MinAttendancePercentage,
                degree.MinLessons,
                degree.MinAge,
                degree.RequiresManualEvaluation,
                degree.CreatedAt
            },
            transaction);
        if (created is null)
        {
            return null;
        }

        transaction.Commit();
        return created;
    }

    public async Task<bool> UpdateGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        GraduationLevel degree,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var belt = await connection.QuerySingleOrDefaultAsync<GraduationBeltState>(
            ProgressionQueries.LockGraduationBelt,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = beltId },
            transaction);
        if (belt is null)
        {
            return false;
        }

        var current = await connection.QuerySingleOrDefaultAsync<GraduationDegreeState>(
            ProgressionQueries.LockGraduationDegree,
            new
            {
                OrganizationId = organizationId,
                SystemId = systemId,
                BeltId = beltId,
                DegreeId = degree.Id
            },
            transaction);
        if (current is null)
        {
            return false;
        }

        await connection.ExecuteAsync(ProgressionQueries.UpdateGraduationDegree, new
        {
            DegreeId = degree.Id,
            BeltId = beltId,
            degree.MinLessons,
            degree.MinTrainingDays,
            degree.MinAttendancePercentage,
            degree.MinAge,
            degree.RequiresManualEvaluation
        }, transaction);
        transaction.Commit();
        return true;
    }

    public async Task<bool> DeleteGraduationDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var belt = await connection.QuerySingleOrDefaultAsync<GraduationBeltState>(
            ProgressionQueries.LockGraduationBelt,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = beltId },
            transaction);
        if (belt is null)
        {
            return false;
        }

        var current = await connection.QuerySingleOrDefaultAsync<GraduationDegreeState>(
            ProgressionQueries.LockGraduationDegree,
            new { OrganizationId = organizationId, SystemId = systemId, BeltId = beltId, DegreeId = degreeId },
            transaction);
        if (current is null || current.DegreeNumber != current.HighestDegree
            || await connection.QuerySingleAsync<bool>(
                ProgressionQueries.GraduationDegreeHasHistory, new { DegreeId = degreeId }, transaction))
        {
            return false;
        }

        var deleted = await connection.QuerySingleOrDefaultAsync<Guid?>(
            ProgressionQueries.DeleteGraduationDegree,
            new { DegreeId = degreeId, BeltId = beltId },
            transaction);
        if (deleted is null)
        {
            return false;
        }

        transaction.Commit();
        return true;
    }

    public async Task<GraduationLevel?> CreateGraduationLevelAsync(Guid organizationId, GraduationLevel level, CancellationToken cancellationToken = default)
    {
        var belt = await CreateGraduationBeltAsync(organizationId, level.GraduationSystemId, new GraduationBelt
        {
            Name = level.Name,
            DegreeCount = 0,
            MinLessons = level.MinLessons,
            MinTrainingDays = level.MinTrainingDays,
            MinAttendancePercentage = level.MinAttendancePercentage,
            RequiresManualEvaluation = level.RequiresManualEvaluation
        }, cancellationToken);
        if (belt is null)
        {
            return null;
        }

        var levels = await ListGraduationLevelsAsync(organizationId, level.GraduationSystemId, cancellationToken);
        return levels.FirstOrDefault(item => item.BeltId == belt.Id && item.DegreeNumber == 0);
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
        if (current is null || current.NextOrder <= current.CurrentOrder || !current.IsEligible
            || (current.RequiresManualEvaluation && !manualEvaluationPassed))
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

    public async Task<IReadOnlyList<FitnessProgressRecord>> ListFitnessProgressRecordsAsync(
        Guid organizationId,
        Guid goalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var records = await connection.QueryAsync<FitnessProgressRecord>(ProgressionQueries.ListFitnessProgressRecords, new
        {
            OrganizationId = organizationId,
            GoalId = goalId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return records.ToList();
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

        public bool IsEligible { get; init; }
    }

    private sealed class GraduationSystemOrder
    {
        public int CurrentOrder { get; init; }

        public bool NameExists { get; init; }
    }

    private sealed class GraduationBeltState
    {
        public string Name { get; init; } = string.Empty;

        public int HierarchyOrder { get; init; }

        public int DegreeCount { get; init; }

        public int CurrentDegree { get; init; }
    }

    private sealed class GraduationDegreeState
    {
        public int DegreeNumber { get; init; }

        public int HighestDegree { get; init; }
    }
}