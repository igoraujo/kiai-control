using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Auth;

namespace KiaiControl.UnitTests;

public sealed class AccessControlUseCaseTests
{
    [Fact]
    public async Task GrantRolePermissionAsync_ShouldProtectOwnerRole()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GrantRolePermissionAsync(
            Guid.NewGuid(), "owner", "clients.manage", Guid.NewGuid(), CancellationToken.None));

        Assert.False(repository.PermissionMutationAttempted);
    }

    [Fact]
    public async Task RevokeRolePermissionAsync_ShouldProtectOwnerRole()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.RevokeRolePermissionAsync(
            Guid.NewGuid(), "owner", "clients.manage", Guid.NewGuid(), CancellationToken.None));

        Assert.False(repository.PermissionMutationAttempted);
    }

    [Fact]
    public async Task GrantRolePermissionAsync_ShouldScopeOverrideToOrganizationAndActor()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository);
        var organizationId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();

        var granted = await useCase.GrantRolePermissionAsync(
            organizationId, "teacher", "lessons.manage", actorUserId, CancellationToken.None);

        Assert.True(granted);
        Assert.Equal(organizationId, repository.LastOrganizationId);
        Assert.Equal(actorUserId, repository.LastActorUserId);
        Assert.Equal("teacher", repository.LastRoleCode);
        Assert.Equal("lessons.manage", repository.LastPermissionCode);
    }

    private sealed class FakeAccessControlRepository : IAccessControlRepository
    {
        public bool PermissionMutationAttempted { get; private set; }

        public Guid LastOrganizationId { get; private set; }

        public Guid LastActorUserId { get; private set; }

        public string? LastRoleCode { get; private set; }

        public string? LastPermissionCode { get; private set; }

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Role>>([]);

        public Task<IReadOnlyList<Permission>> ListPermissionsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Permission>>([]);

        public Task<IReadOnlyList<string>?> ListPersonRolesAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>?>([]);

        public Task<bool> AssignPersonRoleAsync(Guid organizationId, Guid personId, string roleCode, Guid grantedByUserId, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> RevokePersonRoleAsync(Guid organizationId, Guid personId, string roleCode, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<IReadOnlyList<Permission>?> ListRolePermissionsAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken = default)
        {
            LastOrganizationId = organizationId;
            LastRoleCode = roleCode;
            return Task.FromResult<IReadOnlyList<Permission>?>([]);
        }

        public Task<bool> GrantRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            PermissionMutationAttempted = true;
            LastOrganizationId = organizationId;
            LastRoleCode = roleCode;
            LastPermissionCode = permissionCode;
            LastActorUserId = actorUserId;
            return Task.FromResult(true);
        }

        public Task<bool> RevokeRolePermissionAsync(Guid organizationId, string roleCode, string permissionCode, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            PermissionMutationAttempted = true;
            LastOrganizationId = organizationId;
            LastRoleCode = roleCode;
            LastPermissionCode = permissionCode;
            LastActorUserId = actorUserId;
            return Task.FromResult(true);
        }
    }
}