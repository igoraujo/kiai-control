using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Auth;

public sealed class AccessControlUseCase(IAccessControlRepository repository)
{
    private static readonly HashSet<string> RoleCodes = new(StringComparer.Ordinal)
    {
        "client", "teacher", "admin", "financial", "reception", "owner"
    };

    public async Task<IReadOnlyList<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await repository.ListRolesAsync(cancellationToken);
        return roles.Select(role => new RoleResponse
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description
        }).ToList();
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await repository.ListPermissionsAsync(cancellationToken);
        return permissions.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<string>> ListPersonRolesAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        var roles = await repository.ListPersonRolesAsync(organizationId, personId, cancellationToken);
        return roles ?? throw new KeyNotFoundException(SystemMessages.PersonDoesNotExistInThisOrganization);
    }

    public async Task<bool> AssignPersonRoleAsync(
        Guid organizationId,
        Guid personId,
        Guid actorUserId,
        string roleCode,
        bool actorIsOwner,
        CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.ActorUserIdIsRequired);
        }

        roleCode = NormalizeRoleCode(roleCode);
        if (!actorIsOwner && roleCode is "owner" or "admin")
        {
            throw new UnauthorizedAccessException(SystemMessages.OnlyAnOwnerCanGrantOwnerOrAdminRoles);
        }

        return await repository.AssignPersonRoleAsync(organizationId, personId, roleCode, actorUserId, cancellationToken);
    }

    public async Task<bool> RevokePersonRoleAsync(
        Guid organizationId,
        Guid personId,
        string roleCode,
        bool actorIsOwner,
        CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        roleCode = NormalizeRoleCode(roleCode);
        if (!actorIsOwner && roleCode is "owner" or "admin")
        {
            throw new UnauthorizedAccessException(SystemMessages.OnlyAnOwnerCanRevokeOwnerOrAdminRoles);
        }

        return await repository.RevokePersonRoleAsync(organizationId, personId, roleCode, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListRolePermissionsAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }

        roleCode = NormalizeRoleCode(roleCode);
        var permissions = await repository.ListRolePermissionsAsync(organizationId, roleCode, cancellationToken);
        return permissions?.Select(ToResponse).ToList() ?? throw new KeyNotFoundException(SystemMessages.RoleWasNotFound);
    }

    public async Task<bool> GrantRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndActorUserIdAreRequired);
        }

        roleCode = NormalizeRoleCode(roleCode);
        EnsureRolePermissionsEditable(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode) || permissionCode.Trim().Length > 120)
        {
            throw new ArgumentException(SystemMessages.PermissionCodeIsRequiredAndMustNotExceed120Characters);
        }

        return await repository.GrantRolePermissionAsync(organizationId, roleCode, permissionCode.Trim(), actorUserId, cancellationToken);
    }

    public async Task<bool> RevokeRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndActorUserIdAreRequired);
        }

        roleCode = NormalizeRoleCode(roleCode);
        EnsureRolePermissionsEditable(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException(SystemMessages.PermissionCodeIsRequired);
        }

        return await repository.RevokeRolePermissionAsync(organizationId, roleCode, permissionCode.Trim(), actorUserId, cancellationToken);
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        var normalized = roleCode.Trim().ToLowerInvariant();
        return RoleCodes.Contains(normalized) ? normalized : throw new ArgumentException(SystemMessages.RoleCodeIsNotSupported);
    }

    private static void EnsureRolePermissionsEditable(string roleCode)
    {
        if (roleCode == "owner")
        {
            throw new ArgumentException(SystemMessages.TheOwnerRolePermissionsAreManagedByTheSystem);
        }
    }

    private static void EnsureIds(Guid organizationId, Guid personId)
    {
        if (organizationId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndPersonIdAreRequired);
        }
    }

    private static PermissionResponse ToResponse(Core.Entities.Permission permission) => new()
    {
        Id = permission.Id,
        Code = permission.Code,
        Name = permission.Name,
        Description = permission.Description
    };
}