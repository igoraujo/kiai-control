using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAccessControlRepository
{
    Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>?> ListPersonRolesAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default);

    Task<bool> AssignPersonRoleAsync(Guid organizationId, Guid personId, string roleCode, Guid grantedByUserId, CancellationToken cancellationToken = default);

    Task<bool> RevokePersonRoleAsync(Guid organizationId, Guid personId, string roleCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Permission>?> ListRolePermissionsAsync(string roleCode, CancellationToken cancellationToken = default);

    Task<bool> GrantRolePermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default);

    Task<bool> RevokeRolePermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken = default);
}