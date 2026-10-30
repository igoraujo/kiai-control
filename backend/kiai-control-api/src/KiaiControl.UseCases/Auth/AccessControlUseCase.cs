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
        return roles ?? throw new KeyNotFoundException("Person does not exist in this organization.");
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
            throw new ArgumentException("ActorUserId is required.");
        }

        roleCode = NormalizeRoleCode(roleCode);
        if (!actorIsOwner && roleCode is "owner" or "admin")
        {
            throw new UnauthorizedAccessException("Only an owner can grant owner or admin roles.");
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
            throw new UnauthorizedAccessException("Only an owner can revoke owner or admin roles.");
        }

        return await repository.RevokePersonRoleAsync(organizationId, personId, roleCode, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListRolePermissionsAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        roleCode = NormalizeRoleCode(roleCode);
        var permissions = await repository.ListRolePermissionsAsync(organizationId, roleCode, cancellationToken);
        return permissions?.Select(ToResponse).ToList() ?? throw new KeyNotFoundException("Role was not found.");
    }

    public async Task<bool> GrantRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ActorUserId are required.");
        }

        roleCode = NormalizeRoleCode(roleCode);
        EnsureRolePermissionsEditable(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode) || permissionCode.Trim().Length > 120)
        {
            throw new ArgumentException("PermissionCode is required and must not exceed 120 characters.");
        }

        return await repository.GrantRolePermissionAsync(organizationId, roleCode, permissionCode.Trim(), actorUserId, cancellationToken);
    }

    public async Task<bool> RevokeRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ActorUserId are required.");
        }

        roleCode = NormalizeRoleCode(roleCode);
        EnsureRolePermissionsEditable(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("PermissionCode is required.");
        }

        return await repository.RevokeRolePermissionAsync(organizationId, roleCode, permissionCode.Trim(), actorUserId, cancellationToken);
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        var normalized = roleCode.Trim().ToLowerInvariant();
        return RoleCodes.Contains(normalized) ? normalized : throw new ArgumentException("RoleCode is not supported.");
    }

    private static void EnsureRolePermissionsEditable(string roleCode)
    {
        if (roleCode == "owner")
        {
            throw new ArgumentException("The owner role permissions are managed by the system.");
        }
    }

    private static void EnsureIds(Guid organizationId, Guid personId)
    {
        if (organizationId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and PersonId are required.");
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