using Dapper;
using Npgsql;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Administration;

namespace KiaiControl.Repositories.Implementations;
public sealed class SqlAdministrationRepository(IDbConnectionFactory factory) : IAdministrationRepository
{
    public async Task<PagedResult<DirectoryPerson>> ListPeopleAsync(Guid organizationId, int page, int size, string? search, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var result = await c.QueryMultipleAsync(new CommandDefinition(AdministrationQueries.People, new { OrganizationId = organizationId, Offset = (page - 1) * size, Size = size, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%" }, cancellationToken: ct));
        return new((await result.ReadAsync<DirectoryPerson>()).ToArray(), await result.ReadSingleAsync<int>());
    }

    public async Task<Guid> SavePersonAsync(Guid organizationId, Guid? personId, string name, string? email, string? phone, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        try
        {
            return await c.QuerySingleOrDefaultAsync<Guid>(personId is null ? AdministrationQueries.CreatePerson : AdministrationQueries.UpdatePerson, new { OrganizationId = organizationId, Id = personId ?? Guid.NewGuid(), Name = name, Email = email, Phone = phone });
        }
        catch (PostgresException e)when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw PostgresConflictMapper.ToConflict(e);
        }
    }

    public async Task<bool> AddProfileAsync(Guid organizationId, Guid personId, string profile, Guid actorUserId, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var tx = c.BeginTransaction();
        var p = new
        {
            OrganizationId = organizationId,
            PersonId = personId,
            Profile = profile,
            ActorUserId = actorUserId
        };
        var exists = await c.QuerySingleOrDefaultAsync<Guid?>(AdministrationQueries.LockProfilePerson, p, tx);
        if (exists is null)
            return false;
        await c.ExecuteAsync(profile == "client" ? AdministrationQueries.AddClient : AdministrationQueries.AddTeacher, p, tx);
        await c.ExecuteAsync(AdministrationQueries.AddRole, p, tx);
        tx.Commit();
        return true;
    }

    public async Task<bool> ManageAccountAsync(Guid organizationId, Guid personId, Guid actorUserId, bool actorIsOwner, bool? active, string? passwordHash, CancellationToken ct, string? email = null)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var tx = c.BeginTransaction();
        var id = await c.QuerySingleOrDefaultAsync<Guid?>(AdministrationQueries.LockManagedAccount, new { OrganizationId = organizationId, PersonId = personId, ActorUserId = actorUserId, ActorIsOwner = actorIsOwner }, tx);
        if (id is null)
            return false;
        try
        {
            await c.ExecuteAsync(AdministrationQueries.ManageAccount, new { UserId = id, Active = active, PasswordHash = passwordHash, Email = email }, tx);
        }
        catch (PostgresException e)when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw PostgresConflictMapper.ToConflict(e);
        }

        tx.Commit();
        return true;
    }

    public async Task<OrganizationSettings> GetSettingsAsync(Guid organizationId, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        return await c.QuerySingleAsync<OrganizationSettings>(AdministrationQueries.GetSettings, new { OrganizationId = organizationId });
    }

    public async Task SaveSettingsAsync(Guid organizationId, OrganizationSettings settings, CancellationToken ct)
    {
        using var c = await factory.OpenConnectionAsync(ct);
        await c.ExecuteAsync(AdministrationQueries.SaveSettings, new { OrganizationId = organizationId, settings.Timezone, settings.CurrencyCode, settings.AllowClientCheckin, settings.RequireTeacherConfirmation, settings.DefaultLateToleranceMinutes });
    }
}
