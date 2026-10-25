using KiaiControl.Contracts.ClassGroups;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.ClassGroups;

namespace KiaiControl.UnitTests;

public sealed class ClassGroupUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldTrimFieldsAndKeepGroupInOrganization()
    {
        var repository = new InMemoryClassGroupRepository();
        var useCase = new ClassGroupUseCase(repository);
        var organizationId = Guid.NewGuid();

        var created = await useCase.CreateAsync(organizationId, new CreateClassGroupRequest
        {
            ModalityId = Guid.NewGuid(),
            Name = "  Adultos  ",
            Description = "  Treino noturno  ",
            Capacity = 18
        }, CancellationToken.None);
        var organizationGroups = await useCase.ListAsync(organizationId, 1, 50, CancellationToken.None);
        var otherOrganizationGroups = await useCase.ListAsync(Guid.NewGuid(), 1, 50, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Adultos", created.Name);
        Assert.Equal("Treino noturno", created.Description);
        Assert.Equal(18, created.Capacity);
        Assert.Single(organizationGroups);
        Assert.Empty(otherOrganizationGroups);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNonPositiveCapacity()
    {
        var useCase = new ClassGroupUseCase(new InMemoryClassGroupRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(Guid.NewGuid(), new CreateClassGroupRequest
        {
            ModalityId = Guid.NewGuid(),
            Name = "Adultos",
            Capacity = 0
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateScheduleAsync_ShouldRejectInvalidWeekdayOrTimeRange()
    {
        var useCase = new ClassGroupUseCase(new InMemoryClassGroupRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateClassScheduleRequest
        {
            DayOfWeek = 7,
            StartsAt = new TimeOnly(19, 0),
            EndsAt = new TimeOnly(20, 0)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollmentAsync_ShouldRejectEnrollmentWhenClassGroupIsFull()
    {
        var repository = new InMemoryClassGroupRepository();
        var useCase = new ClassGroupUseCase(repository);
        var organizationId = Guid.NewGuid();
        var classGroup = await useCase.CreateAsync(organizationId, new CreateClassGroupRequest
        {
            ModalityId = Guid.NewGuid(),
            Name = "Adultos",
            Capacity = 1
        }, CancellationToken.None);
        Assert.NotNull(classGroup);

        var firstEnrollment = await useCase.CreateEnrollmentAsync(organizationId, classGroup.Id, new CreateEnrollmentRequest
        {
            ClientId = Guid.NewGuid()
        }, CancellationToken.None);
        var secondEnrollment = await useCase.CreateEnrollmentAsync(organizationId, classGroup.Id, new CreateEnrollmentRequest
        {
            ClientId = Guid.NewGuid()
        }, CancellationToken.None);

        Assert.NotNull(firstEnrollment);
        Assert.Null(secondEnrollment);
    }

    [Fact]
    public async Task AssignTeacherAsync_ShouldRejectInvalidAssignmentPeriod()
    {
        var useCase = new ClassGroupUseCase(new InMemoryClassGroupRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.AssignTeacherAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            new DateOnly(2026, 10, 4),
            new DateOnly(2026, 10, 3),
            CancellationToken.None));
    }
}