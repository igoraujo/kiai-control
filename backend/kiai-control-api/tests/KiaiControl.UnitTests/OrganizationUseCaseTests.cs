using KiaiControl.Contracts.Organizations;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Organizations;

namespace KiaiControl.UnitTests;

public sealed class OrganizationUseCaseTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateOrganizationAndOwnerUsingHashedPassword()
    {
        var repository = new FakeOrganizationRepository();
        var useCase = new OrganizationUseCase(repository, new FakePasswordHasher());

        var registration = await useCase.RegisterAsync(new RegisterOrganizationRequest
        {
            Name = "  Kiai Academy  ",
            OwnerFullName = "  Ana Silva  ",
            OwnerEmail = " ANA@EXAMPLE.TEST ",
            OwnerPassword = "LongPassword123"
        }, CancellationToken.None);

        Assert.NotNull(registration);
        Assert.Equal("Kiai Academy", registration.Organization.Name);
        Assert.Equal("ana@example.test", registration.OwnerEmail);
        Assert.Equal("ana@example.test", repository.OwnerEmail);
        Assert.Equal("HASH:LongPassword123", repository.OwnerPasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRejectShortOwnerPassword()
    {
        var useCase = new OrganizationUseCase(new FakeOrganizationRepository(), new FakePasswordHasher());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.RegisterAsync(new RegisterOrganizationRequest
        {
            Name = "Kiai Academy",
            OwnerFullName = "Ana Silva",
            OwnerEmail = "ana@example.test",
            OwnerPassword = "short"
        }, CancellationToken.None));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string input) => $"HASH:{input}";

        public bool Verify(string input, string hash) => hash == Hash(input);
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        public string? OwnerEmail { get; private set; }

        public string? OwnerPasswordHash { get; private set; }

        public Task<(Organization Organization, Guid OwnerUserId)?> RegisterAsync(
            Organization organization,
            string ownerFullName,
            string ownerEmail,
            string ownerPasswordHash,
            CancellationToken cancellationToken = default)
        {
            organization.Id = Guid.NewGuid();
            organization.Status = "active";
            organization.CreatedAt = DateTimeOffset.UtcNow;
            OwnerEmail = ownerEmail;
            OwnerPasswordHash = ownerPasswordHash;
            return Task.FromResult<(Organization Organization, Guid OwnerUserId)?>(
                (organization, Guid.NewGuid()));
        }

        public Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Organization?>(null);
        }

        public Task<Organization?> UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Organization?>(null);
        }
    }
}