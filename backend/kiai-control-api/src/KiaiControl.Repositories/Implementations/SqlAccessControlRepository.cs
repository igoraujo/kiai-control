using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Auth;
using Npgsql;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlAccessControlRepository(IDbConnectionFactory dbConnectionFactory) : IAccessControlRepository
{
    public async Task<IReadOnlyList<UserAccountCandidate>> ListPeopleWithoutAccountsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var candidates = await connection.QueryAsync<UserAccountCandidate>(
            AccessControlQueries.ListPeopleWithoutAccounts,
            new { OrganizationId = organizationId });
        return candidates.ToList();
    }

    public async Task<bool> CreateUserAccountAsync(
        Guid organizationId,
        Guid personId,
        string email,
        string passwordHash,
        string roleCode,
        Guid grantedByUserId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        try
        {
            var createdPersonId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                AccessControlQueries.CreateUserAccount,
                new
                {
                    OrganizationId = organizationId,
                    PersonId = personId,
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleCode = roleCode,
                    GrantedByUserId = grantedByUserId,
                    UserId = Guid.NewGuid()
                },
                transaction);

            if (createdPersonId is null)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            transaction.Rollback();
            return false;
        }
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<Role>(AccessControlQueries.ListRoles);
        return roles.ToList();
    }

    public async Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var permissions = await connection.QueryAsync<Permission>(AccessControlQueries.ListPermissions);
        return permissions.ToList();
    }

    public async Task<IReadOnlyList<string>?> ListPersonRolesAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var exists = await connection.QuerySingleOrDefaultAsync<bool?>(
            AccessControlQueries.PersonExists,
            new { OrganizationId = organizationId, PersonId = personId });
        if (exists is null)
        {
            return null;
        }

        var roles = await connection.QueryAsync<string>(AccessControlQueries.ListPersonRoles, new { OrganizationId = organizationId, PersonId = personId });
        return roles.ToList();
    }

    public async Task<bool> AssignPersonRoleAsync(
        Guid organizationId,
        Guid personId,
        string roleCode,
        Guid grantedByUserId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var assigned = await connection.QuerySingleOrDefaultAsync<Guid?>(AccessControlQueries.AssignPersonRole, new
        {
            OrganizationId = organizationId,
            PersonId = personId,
            RoleCode = roleCode,
            GrantedByUserId = grantedByUserId
        });
        return assigned is not null;
    }

    public async Task<bool> RevokePersonRoleAsync(
        Guid organizationId,
        Guid personId,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AccessControlQueries.RevokePersonRole, new
        {
            OrganizationId = organizationId,
            PersonId = personId,
            RoleCode = roleCode
        });
        return affected > 0;
    }

    public async Task<IReadOnlyList<Permission>?> ListRolePermissionsAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var exists = await connection.QuerySingleOrDefaultAsync<bool?>(AccessControlQueries.RoleExists, new { RoleCode = roleCode });
        if (exists is null)
        {
            return null;
        }

        var permissions = await connection.QueryAsync<Permission>(AccessControlQueries.ListRolePermissions, new
        {
            OrganizationId = organizationId,
            RoleCode = roleCode
        });
        return permissions.ToList();
    }

    public async Task<bool> GrantRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var roleId = await connection.QuerySingleOrDefaultAsync<Guid?>(AccessControlQueries.GrantRolePermission, new
        {
            OrganizationId = organizationId,
            RoleCode = roleCode,
            PermissionCode = permissionCode,
            ActorUserId = actorUserId
        });
        return roleId is not null;
    }

    public async Task<bool> RevokeRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var roleId = await connection.QuerySingleOrDefaultAsync<Guid?>(AccessControlQueries.RevokeRolePermission, new
        {
            OrganizationId = organizationId,
            RoleCode = roleCode,
            PermissionCode = permissionCode,
            ActorUserId = actorUserId
        });
        return roleId is not null;
    }
}
