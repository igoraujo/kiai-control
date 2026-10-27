using KiaiControl.Contracts.Teachers;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Teachers;

namespace KiaiControl.UnitTests;

public sealed class TeacherUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistPersonAndEmploymentFields()
    {
        var repository = new InMemoryTeacherRepository();
        var useCase = new TeacherUseCase(repository, new InMemoryBranchRepository());

        var created = await useCase.CreateAsync(new CreateTeacherRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "  Carlos Lima  ",
            PreferredName = "  Carlos  ",
            Email = "carlos@example.com",
            Phone = "11999990000",
            BirthDate = new DateOnly(1985, 6, 12),
            Document = "12345678909",
            MedicalNotes = "Restricao no joelho",
            EmergencyContactName = "Joana Lima",
            EmergencyContactPhone = "11888880000",
            HideFromRanking = true,
            HiredAt = new DateOnly(2024, 2, 1),
            ActiveFrom = new DateOnly(2024, 2, 15),
            Status = "active"
        }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.PersonId);
        Assert.Equal("Carlos Lima", created.FullName);
        Assert.Equal("Carlos", created.PreferredName);
        Assert.Equal("carlos@example.com", created.Email);
        Assert.Equal("11999990000", created.Phone);
        Assert.Equal(new DateOnly(1985, 6, 12), created.BirthDate);
        Assert.Equal("12345678909", created.Document);
        Assert.Equal("Restricao no joelho", created.MedicalNotes);
        Assert.Equal("Joana Lima", created.EmergencyContactName);
        Assert.Equal("11888880000", created.EmergencyContactPhone);
        Assert.True(created.HideFromRanking);
        Assert.Equal(new DateOnly(2024, 2, 1), created.HiredAt);
        Assert.Equal(new DateOnly(2024, 2, 15), created.ActiveFrom);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnsupportedStatus()
    {
        var useCase = new TeacherUseCase(new InMemoryTeacherRepository(), new InMemoryBranchRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(new CreateTeacherRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "Carlos Lima",
            Status = "unknown"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistPersonAndEmploymentFields()
    {
        var repository = new InMemoryTeacherRepository();
        var useCase = new TeacherUseCase(repository, new InMemoryBranchRepository());
        var organizationId = Guid.NewGuid();
        var created = await useCase.CreateAsync(new CreateTeacherRequest
        {
            OrganizationId = organizationId,
            FullName = "Carlos Lima"
        }, CancellationToken.None);

        var updated = await useCase.UpdateAsync(organizationId, created.Id, new UpdateTeacherRequest
        {
            FullName = "Carlos Lima Filho",
            Email = "carlos.filho@example.com",
            HiredAt = new DateOnly(2023, 5, 1),
            ActiveFrom = new DateOnly(2023, 5, 15),
            Status = "inactive"
        }, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Carlos Lima Filho", updated.FullName);
        Assert.Equal("carlos.filho@example.com", updated.Email);
        Assert.Equal(new DateOnly(2023, 5, 1), updated.HiredAt);
        Assert.Equal(new DateOnly(2023, 5, 15), updated.ActiveFrom);
        Assert.Equal("inactive", updated.Status);
    }
}