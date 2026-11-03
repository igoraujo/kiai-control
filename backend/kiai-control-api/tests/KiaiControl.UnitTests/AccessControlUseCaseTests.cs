using KiaiControl.Contracts.Auth;
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
        var useCase = new AccessControlUseCase(repository, new FakePasswordHasher());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GrantRolePermissionAsync(
            Guid.NewGuid(), "owner", "clients.manage", Guid.NewGuid(), CancellationToken.None));

        Assert.False(repository.PermissionMutationAttempted);
    }

    [Fact]
    public async Task RevokeRolePermissionAsync_ShouldProtectOwnerRole()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository, new FakePasswordHasher());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.RevokeRolePermissionAsync(
            Guid.NewGuid(), "owner", "clients.manage", Guid.NewGuid(), CancellationToken.None));

        Assert.False(repository.PermissionMutationAttempted);
    }

    [Fact]
    public async Task GrantRolePermissionAsync_ShouldScopeOverrideToOrganizationAndActor()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository, new FakePasswordHasher());
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

    [Fact]
    public async Task CreateUserAccountAsync_ShouldNormalizeEmailHashPasswordAndAssignRole()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository, new FakePasswordHasher());
        var personId = Guid.NewGuid();

        var account = await useCase.CreateUserAccountAsync(
            Guid.NewGuid(),
            personId,
            Guid.NewGuid(),
            new CreateUserAccountRequest
            {
                Email = "  TEACHER@EXAMPLE.TEST ",
                TemporaryPassword = "TemporaryPassword123",
                RoleCode = "teacher"
            },
            actorIsOwner: false,
            CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal("teacher@example.test", account.Email);
        Assert.Equal("teacher", account.RoleCode);
        Assert.Equal("teacher@example.test", repository.LastAccountEmail);
        Assert.Equal("HASH:TemporaryPassword123", repository.LastPasswordHash);
        Assert.Equal("teacher", repository.LastAccountRoleCode);
    }

    [Fact]
    public async Task CreateUserAccountAsync_ShouldNotAllowAdminToCreateOwnerAccount()
    {
        var repository = new FakeAccessControlRepository();
        var useCase = new AccessControlUseCase(repository, new FakePasswordHasher());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.CreateUserAccountAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateUserAccountRequest
            {
                Email = "owner@example.test",
                TemporaryPassword = "TemporaryPassword123",
                RoleCode = "owner"
            },
            actorIsOwner: false,
            CancellationToken.None));

        Assert.Null(repository.LastAccountEmail);
    }

    private sealed class FakeAccessControlRepository : IAccessControlRepository
    {
        public bool PermissionMutationAttempted { get; private set; }

        public Guid LastOrganizationId { get; private set; }

        public Guid LastActorUserId { get; private set; }

        public string? LastRoleCode { get; private set; }

        public string? LastPermissionCode { get; private set; }

        public string? LastAccountEmail { get; private set; }

        public string? LastPasswordHash { get; private set; }

        public string? LastAccountRoleCode { get; private set; }

        public Task<IReadOnlyList<UserAccountCandidate>> ListPeopleWithoutAccountsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserAccountCandidate>>([]);

        public Task<bool> CreateUserAccountAsync(
            Guid organizationId,
            Guid personId,
            string email,
            string passwordHash,
            string roleCode,
            Guid grantedByUserId,
            CancellationToken cancellationToken = default)
        {
            LastAccountEmail = email;
            LastPasswordHash = passwordHash;
            LastAccountRoleCode = roleCode;
            return Task.FromResult(true);
        }

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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string input) => $"HASH:{input}";

        public bool Verify(string input, string hash) => hash == Hash(input);
    }
}