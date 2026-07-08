using KiaiControl.Contracts.Students;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Students;

namespace KiaiControl.UnitTests;

public sealed class StudentUseCaseTests
{
    [Fact]
    public async Task CreateStudentAsync_ShouldPersistStudentForOrganization()
    {
        var repository = new InMemoryStudentRepository();
        var useCase = new StudentUseCase(repository);

        var created = await useCase.CreateAsync(new CreateStudentRequest
        {
            OrganizationId = Guid.NewGuid(),
            FullName = "Ana Silva",
            Status = "active"
        }, CancellationToken.None);

        var students = await useCase.ListAsync(created.OrganizationId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Single(students);
        Assert.Equal("Ana Silva", students[0].FullName);
    }
}
