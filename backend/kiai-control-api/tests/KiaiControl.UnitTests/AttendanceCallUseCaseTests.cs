using KiaiControl.Contracts.Attendance;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Attendance;

namespace KiaiControl.UnitTests;

public sealed class AttendanceCallUseCaseTests
{
    [Fact]
    public async Task SaveLessonCallAsync_ShouldRejectDuplicateClientsBeforeSaving()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());
        var clientId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveLessonCallAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SaveAttendanceCallRequest
            {
                Entries =
                [
                    new AttendanceCallEntryRequest { ClientId = clientId, Status = "present" },
                    new AttendanceCallEntryRequest { ClientId = clientId, Status = "absent" }
                ]
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task SaveLessonCallAsync_ShouldRejectUnknownAttendanceStatus()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveLessonCallAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SaveAttendanceCallRequest
            {
                Entries = [new AttendanceCallEntryRequest { ClientId = Guid.NewGuid(), Status = "unknown" }]
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task CheckInAsync_ShouldCreatePendingRecordForAuthenticatedPerson()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());
        var organizationId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var checkedInAt = DateTimeOffset.UtcNow;

        var record = await useCase.CheckInAsync(organizationId, personId, lessonId, checkedInAt, CancellationToken.None);
        var pending = await useCase.ListPendingAsync(organizationId, 1, 50, CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal(personId, record.ClientId);
        Assert.Equal("pending_confirmation", record.Status);
        Assert.Single(pending);
    }

    [Fact]
    public async Task ValidateCheckInAsync_ShouldSetFinalStatusAndValidator()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());
        var organizationId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var validatorPersonId = Guid.NewGuid();
        var checkIn = await useCase.CheckInAsync(organizationId, personId, lessonId, DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.NotNull(checkIn);

        var validated = await useCase.ValidateCheckInAsync(
            organizationId, checkIn.Id, validatorPersonId, "present", DateTimeOffset.UtcNow, CancellationToken.None);
        var pending = await useCase.ListPendingAsync(organizationId, 1, 50, CancellationToken.None);

        Assert.NotNull(validated);
        Assert.Equal("present", validated.Status);
        Assert.Equal(validatorPersonId, validated.ValidatedByPersonId);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task CheckInAsync_ShouldNotCreateDuplicatePendingRecord()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());
        var organizationId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var first = await useCase.CheckInAsync(organizationId, personId, lessonId, DateTimeOffset.UtcNow, CancellationToken.None);
        var duplicate = await useCase.CheckInAsync(organizationId, personId, lessonId, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(duplicate);
    }

    [Fact]
    public async Task SaveLessonCallAsync_ShouldRecordValidatorForManualAttendance()
    {
        var useCase = new AttendanceUseCase(new InMemoryAttendanceRepository());
        var organizationId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var validatorPersonId = Guid.NewGuid();

        var result = await useCase.SaveLessonCallAsync(
            organizationId,
            lessonId,
            validatorPersonId,
            new SaveAttendanceCallRequest
            {
                Entries = [new AttendanceCallEntryRequest
                {
                    ClientId = Guid.NewGuid(),
                    AttendanceDate = DateTimeOffset.UtcNow,
                    Status = "present"
                }]
            },
            CancellationToken.None);

        var record = Assert.Single(result!);
        Assert.Equal("present", record.Status);
        Assert.Equal(validatorPersonId, record.ValidatedByPersonId);
        Assert.NotNull(record.ValidatedAt);
    }
}