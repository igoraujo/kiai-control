using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Entities;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Clients;

namespace KiaiControl.UnitTests;

public sealed class ClientUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistPersonAndClientFields()
    {
        var clientRepository = new InMemoryClientRepository();
        var branchRepository = new InMemoryBranchRepository();
        var organizationId = Guid.NewGuid();
        var branch = new Branch { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "Centro" };
        branchRepository.Add(branch);
        var useCase = new ClientUseCase(clientRepository, branchRepository);

        var created = await useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = organizationId,
            BranchId = branch.Id,
            FullName = " Ana Silva ",
            PreferredName = " Ana ",
            Email = " ANA@EXAMPLE.COM ",
            Phone = " 11999990000 ",
            BirthDate = new DateOnly(1990, 3, 10),
            Document = "123.456.789-09",
            MedicalNotes = " Alergia ",
            EmergencyContactName = " Maria Silva ",
            EmergencyContactPhone = " 11888880000 ",
            HideFromRanking = true,
            RegistrationCode = " A-123 ",
            JoinedAt = new DateOnly(2026, 1, 5),
            Status = "ACTIVE"
        }, CancellationToken.None);

        var found = await useCase.GetByIdAsync(organizationId, created.Id, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.PersonId);
        Assert.Equal(branch.Id, created.BranchId);
        Assert.Equal("Ana Silva", created.FullName);
        Assert.Equal("Ana", created.PreferredName);
        Assert.Equal("ana@example.com", created.Email);
        Assert.Equal("11999990000", created.Phone);
        Assert.Equal(new DateOnly(1990, 3, 10), created.BirthDate);
        Assert.Equal("123.456.789-09", created.Document);
        Assert.Equal("Alergia", created.MedicalNotes);
        Assert.Equal("Maria Silva", created.EmergencyContactName);
        Assert.Equal("11888880000", created.EmergencyContactPhone);
        Assert.True(created.HideFromRanking);
        Assert.Equal("A-123", created.RegistrationCode);
        Assert.Equal(new DateOnly(2026, 1, 5), created.JoinedAt);
        Assert.Equal("active", created.Status);
        Assert.NotNull(found);
        Assert.Equal(created.PersonId, found.PersonId);
        Assert.Equal(created.Email, found.Email);
        Assert.Equal(created.RegistrationCode, found.RegistrationCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidEmail()
    {
        var useCase = new ClientUseCase(new InMemoryClientRepository(), new InMemoryBranchRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "Ana Silva",
            Email = "not-an-email"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistPersonAndClientFields()
    {
        var repository = new InMemoryClientRepository();
        var useCase = new ClientUseCase(repository, new InMemoryBranchRepository());
        var organizationId = Guid.NewGuid();
        var created = await useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = organizationId,
            FullName = "Ana Silva"
        }, CancellationToken.None);

        var updated = await useCase.UpdateAsync(organizationId, created.Id, new UpdateClientRequest
        {
            FullName = "Ana Souza",
            Email = "ana.souza@example.com",
            RegistrationCode = "A-456",
            JoinedAt = new DateOnly(2025, 8, 1),
            Status = "suspended"
        }, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Ana Souza", updated.FullName);
        Assert.Equal("ana.souza@example.com", updated.Email);
        Assert.Equal("A-456", updated.RegistrationCode);
        Assert.Equal(new DateOnly(2025, 8, 1), updated.JoinedAt);
        Assert.Equal("suspended", updated.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectBranchFromAnotherOrganization()
    {
        var branchRepository = new InMemoryBranchRepository();
        var branch = new Branch { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "Outra" };
        branchRepository.Add(branch);
        var useCase = new ClientUseCase(new InMemoryClientRepository(), branchRepository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = Guid.NewGuid(),
            BranchId = branch.Id,
            FullName = "Ana Silva"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ShouldKeepRecordOutOfNormalReads()
    {
        var useCase = new ClientUseCase(new InMemoryClientRepository(), new InMemoryBranchRepository());
        var created = await useCase.CreateAsync(new CreateClientRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "Ana Silva"
        }, CancellationToken.None);

        Assert.True(await useCase.DeleteAsync(created.OrganizationId, created.Id, CancellationToken.None));
        Assert.Empty(await useCase.ListAsync(created.OrganizationId, 1, 50, CancellationToken.None));
        Assert.Null(await useCase.GetByIdAsync(created.OrganizationId, created.Id, CancellationToken.None));
    }
}