using KiaiControl.Contracts.Lessons;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Lessons;

namespace KiaiControl.UnitTests;

public sealed class LessonUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidTimeRange()
    {
        var useCase = new LessonUseCase(new InMemoryLessonRepository());
        var startsAt = DateTimeOffset.Parse("2026-10-03T19:00:00-03:00");

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreateAsync(Guid.NewGuid(), new CreateLessonRequest
        {
            ClassGroupId = Guid.NewGuid(),
            StartsAt = startsAt,
            EndsAt = startsAt
        }, CancellationToken.None));
    }

    [Fact]
    public async Task RescheduleAsync_ShouldMarkLessonAsRescheduled()
    {
        var useCase = new LessonUseCase(new InMemoryLessonRepository());
        var organizationId = Guid.NewGuid();
        var originalStartsAt = DateTimeOffset.Parse("2026-10-03T19:00:00-03:00");
        var created = await useCase.CreateAsync(organizationId, new CreateLessonRequest
        {
            ClassGroupId = Guid.NewGuid(),
            StartsAt = originalStartsAt,
            EndsAt = originalStartsAt.AddHours(1)
        }, CancellationToken.None);
        Assert.NotNull(created);

        var rescheduled = await useCase.RescheduleAsync(organizationId, created.Id, new RescheduleLessonRequest
        {
            StartsAt = originalStartsAt.AddDays(1),
            EndsAt = originalStartsAt.AddDays(1).AddHours(1)
        }, CancellationToken.None);

        Assert.NotNull(rescheduled);
        Assert.Equal("rescheduled", rescheduled.Status);
        Assert.Equal(originalStartsAt.AddDays(1), rescheduled.StartsAt);
        Assert.Equal(TimeSpan.Zero, rescheduled.StartsAt.Offset);
        Assert.Equal(TimeSpan.Zero, rescheduled.EndsAt.Offset);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeTimesToUtc()
    {
        var useCase = new LessonUseCase(new InMemoryLessonRepository());
        var startsAt = DateTimeOffset.Parse("2026-10-03T19:00:00-03:00");

        var created = await useCase.CreateAsync(Guid.NewGuid(), new CreateLessonRequest
        {
            ClassGroupId = Guid.NewGuid(),
            StartsAt = startsAt,
            EndsAt = startsAt.AddHours(1)
        }, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(startsAt, created.StartsAt);
        Assert.Equal(TimeSpan.Zero, created.StartsAt.Offset);
        Assert.Equal(TimeSpan.Zero, created.EndsAt.Offset);
    }

    [Fact]
    public async Task GenerateFromSchedulesAsync_ShouldRejectRangesLongerThanNinetyDays()
    {
        var useCase = new LessonUseCase(new InMemoryLessonRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GenerateFromSchedulesAsync(Guid.NewGuid(), new GenerateLessonsRequest
        {
            ClassGroupId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 4, 2)
        }, CancellationToken.None));
    }
}