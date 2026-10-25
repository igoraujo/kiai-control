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
            new SaveAttendanceCallRequest
            {
                Entries = [new AttendanceCallEntryRequest { ClientId = Guid.NewGuid(), Status = "unknown" }]
            },
            CancellationToken.None));
    }
}