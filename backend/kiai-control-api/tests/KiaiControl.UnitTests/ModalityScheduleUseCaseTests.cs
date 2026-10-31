using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Modalities;

namespace KiaiControl.UnitTests;

public sealed class ModalityScheduleUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateScheduleWithSelectedTeacherAndUnit()
    {
        var repository = new InMemoryModalityScheduleRepository();
        var useCase = new ModalityScheduleUseCase(repository);
        var organizationId = Guid.NewGuid();
        var modalityId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var schedule = await useCase.CreateAsync(organizationId, modalityId, new CreateModalityScheduleRequest
        {
            BranchId = branchId,
            TeacherId = teacherId,
            DayOfWeek = 2,
            StartsAt = new TimeOnly(18, 30),
            EndsAt = new TimeOnly(19, 30)
        }, CancellationToken.None);

        Assert.NotNull(schedule);
        Assert.Equal(organizationId, schedule.OrganizationId);
        Assert.Equal(modalityId, schedule.ModalityId);
        Assert.Equal(branchId, schedule.BranchId);
        Assert.Equal(teacherId, schedule.TeacherId);
        Assert.Equal(new TimeOnly(18, 30), schedule.StartsAt);
        Assert.Equal(new TimeOnly(19, 30), schedule.EndsAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public async Task CreateAsync_ShouldRejectInvalidWeekday(int dayOfWeek)
    {
        var useCase = new ModalityScheduleUseCase(new InMemoryModalityScheduleRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateModalityScheduleRequest
            {
                DayOfWeek = dayOfWeek,
                StartsAt = new TimeOnly(18, 0),
                EndsAt = new TimeOnly(19, 0)
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidInterval()
    {
        var useCase = new ModalityScheduleUseCase(new InMemoryModalityScheduleRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CreateModalityScheduleRequest
            {
                DayOfWeek = 1,
                StartsAt = new TimeOnly(19, 0),
                EndsAt = new TimeOnly(19, 0)
            },
            CancellationToken.None));
    }

    private sealed class InMemoryModalityScheduleRepository : IModalityScheduleRepository
    {
        private readonly List<ModalitySchedule> _schedules = [];

        public Task<ModalitySchedule?> CreateAsync(
            ModalitySchedule schedule,
            CancellationToken cancellationToken = default)
        {
            schedule.Id = Guid.NewGuid();
            schedule.CreatedAt = DateTimeOffset.UtcNow;
            schedule.IsActive = true;
            schedule.BranchName = "Unidade";
            schedule.TeacherName = "Professor";
            _schedules.Add(schedule);
            return Task.FromResult<ModalitySchedule?>(schedule);
        }

        public Task<IReadOnlyList<ModalitySchedule>> ListAsync(
            Guid organizationId,
            Guid modalityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ModalitySchedule>>(_schedules
                .Where(schedule => schedule.OrganizationId == organizationId
                    && schedule.ModalityId == modalityId
                    && schedule.IsActive)
                .ToList());

        public Task<bool> ModalityExistsAsync(
            Guid organizationId,
            Guid modalityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_schedules.Any(schedule =>
                schedule.OrganizationId == organizationId && schedule.ModalityId == modalityId));

        public Task<bool> DeactivateAsync(
            Guid organizationId,
            Guid modalityId,
            Guid scheduleId,
            CancellationToken cancellationToken = default)
        {
            var schedule = _schedules.FirstOrDefault(item =>
                item.OrganizationId == organizationId
                && item.ModalityId == modalityId
                && item.Id == scheduleId
                && item.IsActive);
            if (schedule is null)
            {
                return Task.FromResult(false);
            }

            schedule.IsActive = false;
            return Task.FromResult(true);
        }
    }
}
