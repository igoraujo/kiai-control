using KiaiControl.Contracts.Clients;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Clients;

namespace KiaiControl.UnitTests;

public sealed class ClientUseCaseTests
{
    [Fact]
    public async Task CreateClientAsync_ShouldPersistClientForOrganization()
    {
        var repository = new InMemoryClientRepository();
        var useCase = new ClientUseCase(repository);

        var created = await useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "Ana Silva",
            Status = "active"
        }, CancellationToken.None);

        var clients = await useCase.ListAsync(created.OrganizationId, 1, 50, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Single(clients);
        Assert.Equal("Ana Silva", clients[0].FullName);
    }
}
