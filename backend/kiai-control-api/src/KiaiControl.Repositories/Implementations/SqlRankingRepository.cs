using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Ranking;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlRankingRepository(IDbConnectionFactory dbConnectionFactory) : IRankingRepository
{
    public async Task<RankingCycle?> CreateCycleAsync(RankingCycle cycle, CancellationToken cancellationToken = default)
    {
        cycle.Id = Guid.NewGuid();
        cycle.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<RankingCycle>(RankingQueries.CreateCycle, cycle);
    }

    public async Task<IReadOnlyList<RankingCycle>> ListCyclesAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var cycles = await connection.QueryAsync<RankingCycle>(RankingQueries.ListCycles, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return cycles.ToList();
    }

    public async Task<IReadOnlyList<RankingEntry>?> RecalculateAsync(
        Guid organizationId,
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var cycle = await connection.QuerySingleOrDefaultAsync<RankingCycle>(
            RankingQueries.LockCycle,
            new { OrganizationId = organizationId, CycleId = cycleId },
            transaction);
        if (cycle is null)
        {
            return null;
        }

        await connection.ExecuteAsync(RankingQueries.DeleteCycleEntries, new { CycleId = cycleId }, transaction);
        var entries = await connection.QueryAsync<RankingEntry>(
            RankingQueries.Recalculate,
            new { OrganizationId = organizationId, CycleId = cycleId },
            transaction);
        transaction.Commit();
        return entries.ToList();
    }

    public async Task<IReadOnlyList<RankingEntry>> GetCurrentRankingAsync(
        Guid organizationId,
        DateOnly today,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var entries = await connection.QueryAsync<RankingEntry>(RankingQueries.GetCurrent, new
        {
            OrganizationId = organizationId,
            Today = today,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return entries.ToList();
    }

    public async Task<IReadOnlyList<RankingEntry>> GetRankingByCycleAsync(
        Guid organizationId,
        Guid cycleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var entries = await connection.QueryAsync<RankingEntry>(RankingQueries.GetByCycle, new
        {
            OrganizationId = organizationId,
            CycleId = cycleId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return entries.ToList();
    }

    public async Task<Medal?> CreateMedalAsync(Medal medal, CancellationToken cancellationToken = default)
    {
        medal.Id = Guid.NewGuid();
        medal.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Medal>(RankingQueries.CreateMedal, medal);
    }

    public async Task<IReadOnlyList<Medal>> ListMedalsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<Medal>(RankingQueries.ListMedals, new { OrganizationId = organizationId });
        return items.ToList();
    }

    public async Task<PersonMedal?> AwardMedalAsync(PersonMedal award, CancellationToken cancellationToken = default)
    {
        award.Id = Guid.NewGuid();
        award.AwardedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PersonMedal>(RankingQueries.AwardMedal, award);
    }

    public async Task<IReadOnlyList<PersonMedal>> ListPersonMedalsAsync(
        Guid organizationId,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<PersonMedal>(RankingQueries.ListPersonMedals, new
        {
            OrganizationId = organizationId,
            PersonId = personId
        });
        return items.ToList();
    }

    public async Task<IReadOnlyList<PersonMedal>> ListClientMedalsAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<PersonMedal>(RankingQueries.ListClientMedals, new
        {
            OrganizationId = organizationId,
            ClientId = clientId
        });
        return items.ToList();
    }
}