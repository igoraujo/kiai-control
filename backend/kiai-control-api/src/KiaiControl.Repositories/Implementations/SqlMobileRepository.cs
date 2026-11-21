using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Mobile;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlMobileRepository(IDbConnectionFactory factory) : IMobileRepository
{
    public async Task<MobileMembership?> MembershipAsync(Guid org, Guid person, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.QuerySingleOrDefaultAsync<MobileMembership>(new CommandDefinition(MobileQueries.Membership, new { Org = org, Person = person }, cancellationToken: ct));
    }

    public async Task<MobileAccount?> AccountAsync(Guid org, Guid person, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.QuerySingleOrDefaultAsync<MobileAccount>(new CommandDefinition(MobileQueries.Account, new { Org = org, Person = person }, cancellationToken: ct));
    }

    public async Task<bool> SaveAccountAsync(Guid org, Guid person, MobileAccount account, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        try
        {
            return await c.ExecuteAsync(new CommandDefinition(MobileQueries.SaveAccount, new { Org = org, Person = person, account.FullName, account.PreferredName, account.Email, account.Phone, account.HideFromRanking, account.PushEnabled }, cancellationToken: ct)) > 0;
        }
        catch (Npgsql.PostgresException e) when (e.SqlState == "23505")
        {
            throw new ArgumentException("Este e-mail de contato já está em uso na academia.");
        }
    }

    public async Task<bool> SavePhotoAsync(Guid org, Guid person, byte[]? photo, string? contentType, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.ExecuteAsync(new CommandDefinition(MobileQueries.SavePhoto, new { Org = org, Person = person, Photo = photo, ContentType = contentType }, cancellationToken: ct)) > 0;
    }

    public async Task<MobileProgress> ProgressAsync(Guid org, Guid client, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var r = await c.QueryMultipleAsync(new CommandDefinition(MobileQueries.Progress, new { Org = org, Client = client }, cancellationToken: ct));
        var result = await r.ReadSingleAsync<MobileProgress>();
        result.CurrentStreak = await r.ReadSingleAsync<int>();
        result.Months = (await r.ReadAsync<AttendanceMonth>()).ToArray();
        result.Graduations = (await r.ReadAsync<GraduationProgress>()).ToArray();
        result.TotalPosition = (await RankAsync(org, client, false, 1, 1, true, ct)).FirstOrDefault()?.Position;
        result.YearPosition = (await RankAsync(org, client, true, 1, 1, true, ct)).FirstOrDefault()?.Position;
        return result;
    }

    public Task<IReadOnlyList<AttendanceRanking>> RankingAsync(Guid org, Guid client, bool yearOnly, int page, int size, CancellationToken ct) => RankAsync(org, client, yearOnly, page, size, false, ct);

    private async Task<IReadOnlyList<AttendanceRanking>> RankAsync(Guid org, Guid client, bool yearOnly, int page, int size, bool mineOnly, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return (await c.QueryAsync<AttendanceRanking>(new CommandDefinition(MobileQueries.Ranking, new { Org = org, Client = client, YearOnly = yearOnly, MineOnly = mineOnly, Offset = (page - 1) * size, Size = size }, cancellationToken: ct))).ToArray();
    }

    public async Task<bool> RegisterDeviceAsync(Guid org, Guid person, string installation, string token, string platform, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.ExecuteAsync(new CommandDefinition(MobileQueries.RegisterDevice, new { Org = org, Person = person, Installation = installation, Token = token, Platform = platform }, cancellationToken: ct)) > 0;
    }

    public async Task RemoveDeviceAsync(Guid org, Guid person, string installation, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(MobileQueries.RemoveDevice, new { Org = org, Person = person, Installation = installation }, cancellationToken: ct));
    }
}
