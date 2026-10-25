using KiaiControl.Contracts.Modalities;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Modalities;

namespace KiaiControl.UnitTests;

public sealed class ModalityUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldNormalizeTypeAndKeepModalityInOrganization()
    {
        var repository = new InMemoryModalityRepository();
        var useCase = new ModalityUseCase(repository);
        var organizationId = Guid.NewGuid();

        var created = await useCase.CreateAsync(organizationId, new CreateModalityRequest
        {
            Name = "  Jiu-Jitsu  ",
            Type = "MARTIAL_ART",
            Description = "  Grappling  "
        }, CancellationToken.None);
        var organizationModalities = await useCase.ListAsync(organizationId, 1, 50, CancellationToken.None);
        var otherOrganizationModalities = await useCase.ListAsync(Guid.NewGuid(), 1, 50, CancellationToken.None);

        Assert.Equal("Jiu-Jitsu", created.Name);
        Assert.Equal("martial_art", created.Type);
        Assert.Equal("Grappling", created.Description);
        Assert.Single(organizationModalities);
        Assert.Empty(otherOrganizationModalities);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnsupportedModalityType()
    {
        var useCase = new ModalityUseCase(new InMemoryModalityRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(Guid.NewGuid(), new CreateModalityRequest
        {
            Name = "Boxe",
            Type = "combat_sport"
        }, CancellationToken.None));
    }
}