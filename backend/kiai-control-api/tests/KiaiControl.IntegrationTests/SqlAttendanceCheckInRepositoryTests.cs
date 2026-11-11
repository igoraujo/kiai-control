using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class SqlAttendanceCheckInRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CheckInAsync_ShouldAllowUntilLessonStartAndCancellationBeforeStart()
    {
        var organizationId = Guid.NewGuid();
        var modalityId = Guid.NewGuid();
        var modalityScheduleId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var futureLessonId = Guid.NewGuid();
        var atStartLessonId = Guid.NewGuid();
        var cancellationCutoffLessonId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var futureStartsAt = now.AddMinutes(10);
        var atStartStartsAt = now.AddMinutes(20);
        var cancellationCutoffStartsAt = now.AddMinutes(30);

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into organizations (id, name, status)
                values (@organizationId, @organizationName, 'active');
                insert into modalities (id, organization_id, name, type, is_active)
                values (@modalityId, @organizationId, @modalityName, 'martial_art', true);
                insert into modality_schedules (
                    id, organization_id, modality_id, day_of_week, starts_at, ends_at, is_active
                )
                values (@modalityScheduleId, @organizationId, @modalityId, 1, '08:00', '09:00', true);
                insert into persons (id, organization_id, full_name)
                values (@personId, @organizationId, 'Aluno check-in');
                insert into clients (id, organization_id, person_id, current_status)
                values (@clientId, @organizationId, @personId, 'active');
                insert into person_modalities (person_id, modality_id)
                values (@personId, @modalityId);
                insert into lessons (
                    id, organization_id, modality_id, modality_schedule_id, starts_at, ends_at, status
                )
                values
                    (@futureLessonId, @organizationId, @modalityId, @modalityScheduleId, @futureStartsAt, @futureEndsAt, 'scheduled'),
                    (@atStartLessonId, @organizationId, @modalityId, @modalityScheduleId, @atStartStartsAt, @atStartEndsAt, 'rescheduled'),
                    (@cancellationCutoffLessonId, @organizationId, @modalityId, @modalityScheduleId, @cancellationCutoffStartsAt, @cancellationCutoffEndsAt, 'scheduled');
                """,
                connection);
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("organizationName", $"Check-in integration {organizationId:N}");
            command.Parameters.AddWithValue("modalityId", modalityId);
            command.Parameters.AddWithValue("modalityName", $"Modality {modalityId:N}");
            command.Parameters.AddWithValue("modalityScheduleId", modalityScheduleId);
            command.Parameters.AddWithValue("personId", personId);
            command.Parameters.AddWithValue("clientId", clientId);
            command.Parameters.AddWithValue("futureLessonId", futureLessonId);
            command.Parameters.AddWithValue("atStartLessonId", atStartLessonId);
            command.Parameters.AddWithValue("cancellationCutoffLessonId", cancellationCutoffLessonId);
            command.Parameters.AddWithValue("futureStartsAt", futureStartsAt);
            command.Parameters.AddWithValue("futureEndsAt", futureStartsAt.AddHours(1));
            command.Parameters.AddWithValue("atStartStartsAt", atStartStartsAt);
            command.Parameters.AddWithValue("atStartEndsAt", atStartStartsAt.AddHours(1));
            command.Parameters.AddWithValue("cancellationCutoffStartsAt", cancellationCutoffStartsAt);
            command.Parameters.AddWithValue("cancellationCutoffEndsAt", cancellationCutoffStartsAt.AddHours(1));
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var attendanceRepository = provider.GetRequiredService<IAttendanceRepository>();

        var futureCheckIn = await attendanceRepository.CreateCheckInAsync(
            organizationId, personId, futureLessonId, now);
        var atStartCheckIn = await attendanceRepository.CreateCheckInAsync(
            organizationId, personId, atStartLessonId, atStartStartsAt);
        var afterCutoffCheckIn = await attendanceRepository.CreateCheckInAsync(
            organizationId, personId, cancellationCutoffLessonId, now);
        var checkIns = await attendanceRepository.ListClientCheckInsAsync(
            organizationId, clientId, now, cancellationCutoffStartsAt.AddHours(1));

        Assert.NotNull(futureCheckIn);
        Assert.NotNull(atStartCheckIn);
        Assert.NotNull(afterCutoffCheckIn);
        Assert.Equal(3, checkIns.Count);
        Assert.All(checkIns, checkIn => Assert.Equal("pending_confirmation", checkIn.Status));

        Assert.True(await attendanceRepository.CancelClientCheckInAsync(
            organizationId, personId, futureLessonId, now));
        Assert.True(await attendanceRepository.CancelClientCheckInAsync(
            organizationId, personId, atStartLessonId, atStartStartsAt));
        Assert.False(await attendanceRepository.CancelClientCheckInAsync(
            organizationId, personId, cancellationCutoffLessonId, cancellationCutoffStartsAt.AddSeconds(1)));
        Assert.False(await attendanceRepository.CancelClientCheckInAsync(
            organizationId, Guid.NewGuid(), cancellationCutoffLessonId, now));
    }
}
