using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Clients;

namespace KiaiControl.Repositories.Implementations;
public sealed class SqlClientJourneyRepository(IDbConnectionFactory factory) : IClientJourneyRepository
{
    public async Task<Guid?> GetGoalClientIdAsync(Guid organizationId, Guid goalId, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.QuerySingleOrDefaultAsync<Guid?>(ClientJourneyQueries.GetGoalClientId, new { OrganizationId = organizationId, GoalId = goalId });
    }

    public async Task<ClientJourney?> GetAsync(Guid organizationId, Guid clientId, int page, int size, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var r = await c.QueryMultipleAsync(new CommandDefinition(ClientJourneyQueries.Read, new { OrganizationId = organizationId, ClientId = clientId, Offset = (page - 1) * size, Size = size }, cancellationToken: ct));
        if (!await r.ReadSingleAsync<bool>())
            return null;
        var attendance = (await r.ReadAsync<AttendanceTimelineItem>()).ToArray();
        var count = await r.ReadSingleAsync<int>();
        var grades = (await r.ReadAsync<GraduationTimelineItem>()).ToArray();
        var medals = (await r.ReadAsync<ClientMedalItem>()).ToArray();
        var rankings = (await r.ReadAsync<ClientRankingItem>()).ToArray();
        var totals = await r.ReadSingleAsync<(int PresentCount, int CompletedCount)>();
        return new(new(attendance, count), grades, medals, rankings, totals.PresentCount, totals.CompletedCount);
    }
}
