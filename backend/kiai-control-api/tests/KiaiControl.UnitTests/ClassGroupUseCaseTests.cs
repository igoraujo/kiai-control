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

    [Fact]
    public async Task ListClassGroupDetails_ShouldReturnPersistedChildrenOnlyWithinOrganization()
    {
        var repository = new InMemoryClassGroupRepository();
        var useCase = new ClassGroupUseCase(repository);
        var organizationId = Guid.NewGuid();
        var classGroup = await useCase.CreateAsync(organizationId, new CreateClassGroupRequest
        {
            ModalityId = Guid.NewGuid(),
            Name = "Adultos",
            Capacity = 12
        }, CancellationToken.None);
        Assert.NotNull(classGroup);

        var schedule = await useCase.CreateScheduleAsync(organizationId, classGroup.Id, new CreateClassScheduleRequest
        {
            DayOfWeek = 1,
            StartsAt = new TimeOnly(18, 0),
            EndsAt = new TimeOnly(19, 0)
        }, CancellationToken.None);
        var enrollment = await useCase.CreateEnrollmentAsync(organizationId, classGroup.Id, new CreateEnrollmentRequest
        {
            ClientId = Guid.NewGuid()
        }, CancellationToken.None);
        var teacherId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 10, 5);
        var assigned = await useCase.AssignTeacherAsync(
            organizationId, classGroup.Id, teacherId, true, startDate, null, CancellationToken.None);

        var schedules = await useCase.ListSchedulesAsync(organizationId, classGroup.Id, CancellationToken.None);
        var enrollments = await useCase.ListEnrollmentsAsync(organizationId, classGroup.Id, CancellationToken.None);
        var assignments = await useCase.ListTeacherAssignmentsAsync(organizationId, classGroup.Id, CancellationToken.None);
        var otherOrganizationSchedules = await useCase.ListSchedulesAsync(Guid.NewGuid(), classGroup.Id, CancellationToken.None);
        var otherOrganizationEnrollments = await useCase.ListEnrollmentsAsync(Guid.NewGuid(), classGroup.Id, CancellationToken.None);
        var otherOrganizationAssignments = await useCase.ListTeacherAssignmentsAsync(Guid.NewGuid(), classGroup.Id, CancellationToken.None);

        Assert.NotNull(schedule);
        Assert.NotNull(enrollment);
        Assert.True(assigned);
        Assert.Single(schedules);
        Assert.Equal(schedule.Id, schedules[0].Id);
        Assert.Single(enrollments);
        Assert.Equal(enrollment.Id, enrollments[0].Id);
        Assert.Single(assignments);
        Assert.Equal(teacherId, assignments[0].TeacherId);
        Assert.Equal(startDate, assignments[0].StartsAt);
        Assert.True(assignments[0].IsPrimary);
        Assert.Empty(otherOrganizationSchedules);
        Assert.Empty(otherOrganizationEnrollments);
        Assert.Empty(otherOrganizationAssignments);
    }
}