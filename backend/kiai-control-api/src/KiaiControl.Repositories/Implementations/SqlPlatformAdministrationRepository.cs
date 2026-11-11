using System.Data;
using System.Text.Json;
using Dapper;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Audit;
using KiaiControl.Repositories.Queries.Organizations;
using KiaiControl.Repositories.Queries.Platform;
using Npgsql;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlPlatformAdministrationRepository(IDbConnectionFactory factory) : IPlatformAdministrationRepository
{
    public async Task<PagedResult<Organization>> ListOrganizationsAsync(int page, int pageSize, string? search, string? status, CancellationToken ct, string archive = "active")
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformQueries.Organizations,
            new { PlatformOrganizationId = PlatformIdentity.OrganizationId, Offset = (page - 1) * pageSize, PageSize = pageSize, Search = search, Status = status, Archive = archive }, cancellationToken: ct));
        return new((await result.ReadAsync<Organization>()).ToArray(), await result.ReadSingleAsync<int>());
    }

    public async Task<Organization?> GetOrganizationAsync(Guid organizationId, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Organization>(new CommandDefinition(PlatformLifecycleQueries.GetOrganization,
            new { OrganizationId = organizationId, PlatformOrganizationId = PlatformIdentity.OrganizationId }, cancellationToken: ct));
    }

    public Task<bool> ChangeArchiveAsync(Guid organizationId, bool archived, string confirmedName, AuditLog audit, CancellationToken ct, bool reactivate = false) =>
        MutateAsync(organizationId, audit, async (connection, transaction, before) =>
        {
            if (!string.Equals(before.Name, confirmedName.Trim(), StringComparison.Ordinal))
                throw new ConflictException("Digite o nome da academia exatamente como exibido para confirmar.");
            if ((before.DeletedAt is not null) == archived)
                throw new ConflictException(archived ? "A academia já está arquivada." : "A academia não está arquivada.");
            var status = !archived && reactivate ? "active" : "inactive";
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.Status,
                new { OrganizationId = organizationId, Status = status }, transaction, cancellationToken: ct));
            await connection.ExecuteAsync(new CommandDefinition(PlatformLifecycleQueries.Archive,
                new { OrganizationId = organizationId, Archived = archived, Status = status }, transaction, cancellationToken: ct));
            EnrichAudit(audit, new { Archived = before.DeletedAt is not null, before.Status }, new { Archived = archived, Status = status, SessionsRevoked = true });
            return true;
        }, ct, allowArchived: true);

    public Task<bool> SetPrivacyHoldAsync(Guid organizationId, bool hold, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, before) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(PlatformLifecycleQueries.PrivacyHold,
                new { OrganizationId = organizationId, Hold = hold }, transaction, cancellationToken: ct));
            EnrichAudit(audit, new { before.PrivacyHold }, new { PrivacyHold = hold });
            return true;
        }, ct, allowArchived: true);

    public async Task<AuditLog?> GetAuditAsync(Guid organizationId, Guid auditId, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<AuditLog>(new CommandDefinition(PlatformLifecycleQueries.AuditById,
            new { OrganizationId = organizationId, AuditId = auditId }, cancellationToken: ct));
    }

    public Task<bool> UpdateOrganizationAsync(Organization organization, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organization.Id, audit, async (connection, transaction, before) =>
        {
            organization.Status = before.Status;
            organization.CreatedAt = before.CreatedAt;
            organization.PrivacyHold = before.PrivacyHold;
            organization.DeletedAt = before.DeletedAt;
            organization.UpdatedAt = DateTimeOffset.UtcNow;
            await connection.ExecuteAsync(new CommandDefinition(OrganizationQueries.Update, organization, transaction, cancellationToken: ct));
            EnrichAudit(audit, before, organization);
            return true;
        }, ct);

    public Task<bool> SetOrganizationStatusAsync(Guid organizationId, string status, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, before) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.Status, new { OrganizationId = organizationId, Status = status }, transaction, cancellationToken: ct));
            EnrichAudit(audit, new { before.Status }, new { Status = status, SessionsRevoked = true });
            return true;
        }, ct);

    public async Task<PagedResult<PlatformAccount>> ListAccountsAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformQueries.Accounts,
            new { OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize, Search = search }, cancellationToken: ct));
        return new((await result.ReadAsync<PlatformAccount>()).ToArray(), await result.ReadSingleAsync<int>());
    }

    public Task<bool> CreateAccountAsync(Guid organizationId, string name, string email, string passwordHash, string role, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, _) =>
        {
            var userId = Guid.NewGuid();
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.CreateAccount, new
            {
                OrganizationId = organizationId, PersonId = Guid.NewGuid(), UserId = userId,
                FullName = name, Email = email, PasswordHash = passwordHash, Role = role,
                ActorUserId = audit.ActorUserAccountId
            }, transaction, cancellationToken: ct));
            audit.EntityId = userId;
            EnrichAudit(audit, null, new { FullName = name, Email = email, Role = role, MustChangePassword = true });
            return true;
        }, ct);

    public Task<bool> UpdateAccountAsync(Guid organizationId, Guid userId, string email, string[] roles, bool active, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, _) =>
        {
            var before = await LockAccountAsync(connection, transaction, organizationId, userId, audit, ct);
            if (before is null)
                return false;

            var parameters = new { OrganizationId = organizationId, UserId = userId, before.PersonId, Email = email, Roles = roles, Active = active, ActorUserId = audit.ActorUserAccountId };
            if (!await connection.QuerySingleAsync<bool>(new CommandDefinition(PlatformQueries.ProfilesValid, parameters, transaction, cancellationToken: ct)))
                throw new ConflictException("Os perfis de aluno e professor exigem o respectivo cadastro na academia.");

            if (before.IsActive && before.Roles.Contains("owner") && (!active || !roles.Contains("owner")))
            {
                var otherOwners = await connection.QuerySingleAsync<int>(new CommandDefinition(PlatformQueries.OtherActiveOwners, parameters, transaction, cancellationToken: ct));
                if (otherOwners == 0)
                    throw new ConflictException("Adicione outro responsável ativo antes de bloquear ou remover o perfil do último owner.");
            }

            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.UpdateAccount, parameters, transaction, cancellationToken: ct));
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.RevokeSessions, parameters, transaction, cancellationToken: ct));
            EnrichAudit(audit, new { before.Email, before.IsActive, before.Roles }, new { Email = email, IsActive = active, Roles = roles, SessionsRevoked = true });
            return true;
        }, ct);

    public Task<bool> ResetPasswordAsync(Guid organizationId, Guid userId, string passwordHash, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, _) =>
        {
            if (await LockAccountAsync(connection, transaction, organizationId, userId, audit, ct) is null)
                return false;
            var parameters = new { UserId = userId, PasswordHash = passwordHash };
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.ResetPassword, parameters, transaction, cancellationToken: ct));
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.RevokeSessions, parameters, transaction, cancellationToken: ct));
            EnrichAudit(audit, null, new { MustChangePassword = true, SessionsRevoked = true });
            return true;
        }, ct);

    public Task<bool> RevokeSessionsAsync(Guid organizationId, Guid userId, AuditLog audit, CancellationToken ct) =>
        MutateAsync(organizationId, audit, async (connection, transaction, _) =>
        {
            if (await LockAccountAsync(connection, transaction, organizationId, userId, audit, ct) is null)
                return false;
            await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.RevokeSessions, new { UserId = userId }, transaction, cancellationToken: ct));
            EnrichAudit(audit, null, new { SessionsRevoked = true });
            return true;
        }, ct);

    public async Task<PagedResult<AuditLog>> ListAuditAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformQueries.Audit, LogParameters(filter), cancellationToken: ct));
        return new((await result.ReadAsync<AuditLog>()).ToArray(), await result.ReadSingleAsync<int>());
    }

    public async Task<PagedResult<PlatformIncident>> ListIncidentsAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(PlatformQueries.Incidents, LogParameters(filter), cancellationToken: ct));
        return new((await result.ReadAsync<PlatformIncident>()).ToArray(), await result.ReadSingleAsync<int>());
    }

    public async Task<PlatformOverview> GetOverviewAsync(CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        return await connection.QuerySingleAsync<PlatformOverview>(new CommandDefinition(PlatformQueries.Overview,
            new { PlatformOrganizationId = PlatformIdentity.OrganizationId }, cancellationToken: ct));
    }

    public async Task RecordIncidentAsync(PlatformIncident incident, CancellationToken ct)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(PlatformQueries.InsertIncident, incident, cancellationToken: ct));
    }

    private async Task<bool> MutateAsync(Guid organizationId, AuditLog audit, Func<IDbConnection, IDbTransaction, Organization, Task<bool>> action, CancellationToken ct, bool allowArchived = false)
    {
        using var connection = await factory.OpenConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();
        try
        {
            var organization = await connection.QuerySingleOrDefaultAsync<Organization>(new CommandDefinition(PlatformQueries.LockOrganization,
                new { OrganizationId = organizationId, PlatformOrganizationId = PlatformIdentity.OrganizationId, AllowArchived = allowArchived }, transaction, cancellationToken: ct));
            if (organization is null || !await action(connection, transaction, organization))
                return false;

            audit.Id = Guid.NewGuid();
            audit.CreatedAt = DateTimeOffset.UtcNow;
            audit.OrganizationId = organizationId;
            await connection.ExecuteAsync(new CommandDefinition(AuditQueries.Insert, audit, transaction, cancellationToken: ct));
            transaction.Commit();
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw PostgresConflictMapper.ToConflict(exception);
        }
    }

    private static Task<PlatformAccount?> LockAccountAsync(IDbConnection connection, IDbTransaction transaction, Guid organizationId, Guid userId, AuditLog audit, CancellationToken ct) =>
        connection.QuerySingleOrDefaultAsync<PlatformAccount>(new CommandDefinition(PlatformQueries.LockAccount,
            new { OrganizationId = organizationId, UserId = userId, ActorUserId = audit.ActorUserAccountId }, transaction, cancellationToken: ct));

    private static void EnrichAudit(AuditLog audit, object? before, object? after) =>
        audit.Metadata = JsonSerializer.Serialize(new
        {
            context = JsonSerializer.Deserialize<JsonElement>(audit.Metadata ?? "{}"), before, after
        });

    private static object LogParameters(PlatformLogFilter filter) => new
    {
        filter.OrganizationId, filter.ActorUserId, filter.ActorSearch, filter.Action,
        From = filter.From.ToUniversalTime(), To = filter.To.ToUniversalTime(),
        Offset = (filter.Page - 1) * filter.PageSize, filter.PageSize
    };
}
