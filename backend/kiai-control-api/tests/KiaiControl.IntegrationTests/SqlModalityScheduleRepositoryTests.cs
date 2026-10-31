using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class SqlModalityScheduleRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Migration_ShouldRemoveClassGroupSchema()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            select to_regclass('public.class_groups') is null
                and to_regclass('public.class_group_teachers') is null
                and to_regclass('public.class_schedules') is null
                and to_regclass('public.enrollments') is null
                and to_regtype('public.class_group_status') is null
                and to_regtype('public.enrollment_status') is null
                and not exists (
                    select 1
                    from information_schema.columns
                    where table_schema = 'public'
                      and table_name in ('lessons', 'ranking_cycles')
                      and column_name = 'class_group_id'
                );
            """,
            connection);

        Assert.True((bool)(await command.ExecuteScalarAsync())!);

        await using var conversionCommand = new NpgsqlCommand(
            """
            select
                (select modality_id = '00000000-0000-0000-0000-000000000702'
                 from lessons where id = '00000000-0000-0000-0000-000000000706')
                and (select modality_id = '00000000-0000-0000-0000-000000000702'
                     from ranking_cycles where id = '00000000-0000-0000-0000-000000000708')
                and exists (
                    select 1 from person_modalities
                    where person_id = '00000000-0000-0000-0000-000000000703'
                      and modality_id = '00000000-0000-0000-0000-000000000702'
                      and linked_at = '2025-01-02T10:00:00Z'
                )
                and exists (
                    select 1 from attendances where id = '00000000-0000-0000-0000-000000000707'
                );
            """,
            connection);

        Assert.True((bool)(await conversionCommand.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task GenerateAndCall_ShouldUseOpenModalityMembershipAndKeepGeneratedLessonsWhenScheduleIsRemoved()
    {
        var organizationId = Guid.NewGuid();
        var modalityId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var authorizedClientId = Guid.NewGuid();
        var unauthorizedClientId = Guid.NewGuid();
        var inactiveClientId = Guid.NewGuid();
        var authorizedPersonId = Guid.NewGuid();
        var unauthorizedPersonId = Guid.NewGuid();
        var inactivePersonId = Guid.NewGuid();
        var teacherPersonId = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into organizations (id, name, status) values (@organizationId, @name, 'active');
                insert into modalities (id, organization_id, name, type, is_active)
                values (@modalityId, @organizationId, @modalityName, 'martial_art', true);
                insert into persons (id, organization_id, full_name) values
                    (@teacherPersonId, @organizationId, 'Professor Teste'),
                    (@authorizedPersonId, @organizationId, 'Aluno Vinculado'),
                    (@unauthorizedPersonId, @organizationId, 'Aluno Sem Modalidade'),
                    (@inactivePersonId, @organizationId, 'Aluno Inativo');
                insert into teachers (id, organization_id, person_id, current_status)
                values (@teacherId, @organizationId, @teacherPersonId, 'active');
                insert into clients (id, organization_id, person_id, current_status) values
                    (@authorizedClientId, @organizationId, @authorizedPersonId, 'active'),
                    (@unauthorizedClientId, @organizationId, @unauthorizedPersonId, 'active'),
                    (@inactiveClientId, @organizationId, @inactivePersonId, 'inactive');
                insert into person_modalities (person_id, modality_id) values
                    (@authorizedPersonId, @modalityId),
                    (@inactivePersonId, @modalityId);
                """,
                connection);
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("name", $"Open modality integration {organizationId:N}");
            command.Parameters.AddWithValue("modalityId", modalityId);
            command.Parameters.AddWithValue("modalityName", $"Karate {modalityId:N}");
            command.Parameters.AddWithValue("teacherId", teacherId);
            command.Parameters.AddWithValue("teacherPersonId", teacherPersonId);
            command.Parameters.AddWithValue("authorizedClientId", authorizedClientId);
            command.Parameters.AddWithValue("unauthorizedClientId", unauthorizedClientId);
            command.Parameters.AddWithValue("inactiveClientId", inactiveClientId);
            command.Parameters.AddWithValue("authorizedPersonId", authorizedPersonId);
            command.Parameters.AddWithValue("unauthorizedPersonId", unauthorizedPersonId);
            command.Parameters.AddWithValue("inactivePersonId", inactivePersonId);
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var scheduleRepository = provider.GetRequiredService<IModalityScheduleRepository>();
        var lessonRepository = provider.GetRequiredService<ILessonRepository>();
        var attendanceRepository = provider.GetRequiredService<IAttendanceRepository>();
        var reportRepository = provider.GetRequiredService<IReportRepository>();
        var rankingRepository = provider.GetRequiredService<IRankingRepository>();
        var progressionRepository = provider.GetRequiredService<IProgressionRepository>();

        var schedule = await scheduleRepository.CreateAsync(new ModalitySchedule
        {
            OrganizationId = organizationId,
            ModalityId = modalityId,
            TeacherId = teacherId,
            DayOfWeek = 1,
            StartsAt = new TimeSpan(18, 0, 0),
            EndsAt = new TimeSpan(19, 0, 0)
        });

        Assert.NotNull(schedule);
        var listedSchedules = await scheduleRepository.ListAsync(organizationId, modalityId);
        Assert.Equal("Professor Teste", Assert.Single(listedSchedules).TeacherName);

        var firstGeneration = await lessonRepository.GenerateFromModalitySchedulesAsync(
            organizationId, modalityId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));
        var repeatedGeneration = await lessonRepository.GenerateFromModalitySchedulesAsync(
            organizationId, modalityId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.NotNull(firstGeneration);
        Assert.Single(firstGeneration);
        Assert.Empty(repeatedGeneration!);
        Assert.Equal(modalityId, firstGeneration[0].ModalityId);
        Assert.Equal("Karate " + modalityId.ToString("N"), firstGeneration[0].ModalityName);

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                select count(*)
                from lesson_instructors
                where lesson_id = @lessonId and teacher_id = @teacherId and is_substitute = false;
                """,
                connection);
            command.Parameters.AddWithValue("lessonId", firstGeneration[0].Id);
            command.Parameters.AddWithValue("teacherId", teacherId);
            Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
        }

        var eligibleClients = await attendanceRepository.ListEligibleClientsByLessonAsync(
            organizationId, firstGeneration[0].Id);
        Assert.Collection(eligibleClients, client =>
        {
            Assert.Equal(authorizedClientId, client.ClientId);
            Assert.Equal("Aluno Vinculado", client.ClientName);
        });

        var savedCall = await attendanceRepository.SaveLessonCallAsync(
            organizationId,
            firstGeneration[0].Id,
            teacherPersonId,
            [
                new AttendanceRecord
                {
                    ClientId = authorizedClientId,
                    AttendanceDate = firstGeneration[0].StartsAt,
                    Status = "present"
                }
            ]);
        Assert.Single(savedCall!);

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "update lessons set status = 'completed' where id = @lessonId",
                connection);
            command.Parameters.AddWithValue("lessonId", firstGeneration[0].Id);
            await command.ExecuteNonQueryAsync();
        }

        var attendanceReport = await reportRepository.GetAttendanceAsync(
            organizationId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), modalityId);
        var modalityAttendance = Assert.Single(attendanceReport);
        Assert.Equal(modalityId, modalityAttendance.ModalityId);
        Assert.Equal(1, modalityAttendance.AttendanceCount);
        Assert.Equal(100m, modalityAttendance.AttendanceRate);

        var performanceReport = await reportRepository.GetClientPerformanceAsync(
            organizationId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), modalityId, 1, 20);
        var performance = Assert.Single(performanceReport);
        Assert.Equal(authorizedClientId, performance.ClientId);
        Assert.Equal(1, performance.AttendanceCount);
        Assert.Equal(1, performance.LessonsAvailable);

        var cycle = await rankingRepository.CreateCycleAsync(new RankingCycle
        {
            OrganizationId = organizationId,
            ModalityId = modalityId,
            Name = "Open modality integration",
            StartsAt = new DateOnly(2026, 1, 5),
            EndsAt = new DateOnly(2026, 1, 11)
        });
        Assert.NotNull(cycle);
        var ranking = await rankingRepository.RecalculateAsync(organizationId, cycle.Id);
        Assert.Equal(authorizedClientId, Assert.Single(ranking!).ClientId);

        var systemId = Guid.NewGuid();
        var currentLevelId = Guid.NewGuid();
        var nextLevelId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into graduation_systems (id, organization_id, modality_id, name)
                values (@systemId, @organizationId, @modalityId, 'Test graduation');
                insert into graduation_levels (id, graduation_system_id, name, hierarchy_order)
                values (@currentLevelId, @systemId, 'White', 1);
                insert into graduation_levels (
                    id, graduation_system_id, name, hierarchy_order, min_training_days, min_lessons
                )
                values (@nextLevelId, @systemId, 'Yellow', 2, 1, 1);
                insert into client_graduations (
                    id, organization_id, client_id, modality_id, current_level_id, granted_at
                )
                values (@graduationId, @organizationId, @clientId, @modalityId, @currentLevelId, current_date);
                """,
                connection);
            command.Parameters.AddWithValue("systemId", systemId);
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("modalityId", modalityId);
            command.Parameters.AddWithValue("currentLevelId", currentLevelId);
            command.Parameters.AddWithValue("nextLevelId", nextLevelId);
            command.Parameters.AddWithValue("graduationId", Guid.NewGuid());
            command.Parameters.AddWithValue("clientId", authorizedClientId);
            await command.ExecuteNonQueryAsync();
        }

        var eligibleForGraduation = await progressionRepository.ListEligibleClientsAsync(organizationId, systemId);
        Assert.Equal(authorizedClientId, Assert.Single(eligibleForGraduation).ClientId);

        Assert.True(await scheduleRepository.DeactivateAsync(organizationId, modalityId, schedule.Id));
        Assert.Empty(await scheduleRepository.ListAsync(organizationId, modalityId));
        Assert.NotNull(await lessonRepository.GetByIdAsync(organizationId, firstGeneration[0].Id));
    }
}
