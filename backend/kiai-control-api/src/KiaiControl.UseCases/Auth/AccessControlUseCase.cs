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

    public async Task<IReadOnlyList<PermissionResponse>> ListRolePermissionsAsync(string roleCode, CancellationToken cancellationToken)
    {
        roleCode = NormalizeRoleCode(roleCode);
        var permissions = await repository.ListRolePermissionsAsync(roleCode, cancellationToken);
        return permissions?.Select(ToResponse).ToList() ?? throw new KeyNotFoundException("Role was not found.");
    }

    public async Task<bool> GrantRolePermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken)
    {
        roleCode = NormalizeRoleCode(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode) || permissionCode.Trim().Length > 120)
        {
            throw new ArgumentException("PermissionCode is required and must not exceed 120 characters.");
        }

        return await repository.GrantRolePermissionAsync(roleCode, permissionCode.Trim(), cancellationToken);
    }

    public async Task<bool> RevokeRolePermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken)
    {
        roleCode = NormalizeRoleCode(roleCode);
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("PermissionCode is required.");
        }

        return await repository.RevokeRolePermissionAsync(roleCode, permissionCode.Trim(), cancellationToken);
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        var normalized = roleCode.Trim().ToLowerInvariant();
        return RoleCodes.Contains(normalized) ? normalized : throw new ArgumentException("RoleCode is not supported.");
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