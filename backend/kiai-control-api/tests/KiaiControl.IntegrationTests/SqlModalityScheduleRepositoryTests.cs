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
    public async Task ListActiveTypesAsync_ShouldReturnDistinctActiveTypesWithinOrganization()
    {
        var organizationId = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into organizations (id, name, status)
                values (@organizationId, @name, 'active');
                insert into modalities (id, organization_id, name, type, is_active)
                values
                    (@martialId, @organizationId, 'Jiu-Jitsu', 'martial_art', true),
                    (@secondMartialId, @organizationId, 'Karate', 'martial_art', true),
                    (@fitnessId, @organizationId, 'Musculação', 'fitness', true),
                    (@inactiveId, @organizationId, 'Fitness inativo', 'fitness', false);
                """,
                connection);
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("name", $"Capabilities integration {organizationId:N}");
            command.Parameters.AddWithValue("martialId", Guid.NewGuid());
            command.Parameters.AddWithValue("secondMartialId", Guid.NewGuid());
            command.Parameters.AddWithValue("fitnessId", Guid.NewGuid());
            command.Parameters.AddWithValue("inactiveId", Guid.NewGuid());
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var modalityRepository = provider.GetRequiredService<IModalityRepository>();

        var activeTypes = await modalityRepository.ListActiveTypesAsync(organizationId);

        Assert.Equal(["fitness", "martial_art"], activeTypes.Order(StringComparer.Ordinal));
        Assert.Empty(await modalityRepository.ListActiveTypesAsync(Guid.NewGuid()));
    }

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

        await using var graduationMigrationCommand = new NpgsqlCommand(
            """
            select
                (select belt.name = 'Legacy white' and belt.degree_count = 0
                 from graduation_belts belt
                 where belt.id = (
                     select belt_id from graduation_levels
                     where id = '00000000-0000-0000-0000-000000000711'
                 ))
                and (select degree_number = 0 and hierarchy_order = 200
                     from graduation_levels where id = '00000000-0000-0000-0000-000000000712')
                and (select min_lessons = 20 from graduation_levels
                     where id = '00000000-0000-0000-0000-000000000712');
            """,
            connection);
        Assert.True((bool)(await graduationMigrationCommand.ExecuteScalarAsync())!);

        await using var invalidGraduationCleanupCommand = new NpgsqlCommand(
            """
            select not exists (
                       select 1 from graduation_systems
                       where id = '00000000-0000-0000-0000-000000000714'
                   )
                and not exists (
                       select 1 from graduation_belts
                       where graduation_system_id = '00000000-0000-0000-0000-000000000714'
                   )
                and not exists (
                       select 1 from graduation_levels
                       where id = '00000000-0000-0000-0000-000000000715'
                   )
                and not exists (
                       select 1 from client_graduations
                       where id in (
                           '00000000-0000-0000-0000-000000000716',
                           '00000000-0000-0000-0000-000000000718'
                       )
                   )
                and not exists (
                       select 1 from graduation_events
                       where id in (
                           '00000000-0000-0000-0000-000000000717',
                           '00000000-0000-0000-0000-000000000719'
                       )
                   )
                and exists (
                       select 1 from graduation_systems
                       where id = '00000000-0000-0000-0000-000000000710'
                   )
                and exists (
                       select 1 from graduation_levels
                       where id = '00000000-0000-0000-0000-000000000712'
                   );
            """,
            connection);
        Assert.True((bool)(await invalidGraduationCleanupCommand.ExecuteScalarAsync())!);

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
        var notificationRepository = provider.GetRequiredService<INotificationRepository>();
        var auditLogRepository = provider.GetRequiredService<IAuditLogRepository>();

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
        var teacherSchedules = await scheduleRepository.ListByTeacherAsync(organizationId, teacherId);
        Assert.Equal(modalityId, Assert.Single(teacherSchedules).ModalityId);
        Assert.Equal($"Karate {modalityId:N}", teacherSchedules[0].ModalityName);

        var firstGeneration = await lessonRepository.GenerateFromModalitySchedulesAsync(
            organizationId, modalityId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));
        var repeatedGeneration = await lessonRepository.GenerateFromModalitySchedulesAsync(
            organizationId, modalityId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.NotNull(firstGeneration);
        Assert.Single(firstGeneration);
        Assert.Empty(repeatedGeneration!);
        Assert.Equal(modalityId, firstGeneration[0].ModalityId);
        Assert.Equal("Karate " + modalityId.ToString("N"), firstGeneration[0].ModalityName);
        var teacherAgenda = await lessonRepository.ListByOrganizationAsync(
            organizationId,
            1,
            50,
            teacherId: teacherId,
            from: new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero),
            to: new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal(firstGeneration[0].Id, Assert.Single(teacherAgenda).Id);

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                select count(1)
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

        var dashboard = await reportRepository.GetDashboardAsync(
            organizationId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5));
        Assert.Equal(1, dashboard.CompletedLessons);
        Assert.Equal(100m, dashboard.AttendanceRate);

        var financialReport = await reportRepository.GetFinancialAsync(
            organizationId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5));
        Assert.Equal(0m, financialReport.AmountIssued);
        Assert.Equal(0, financialReport.InvoicesIssued);

        var graduationReport = await reportRepository.GetGraduationAsync(
            organizationId, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), modalityId);
        var modalityGraduation = Assert.Single(graduationReport);
        Assert.Equal(modalityId, modalityGraduation.ModalityId);
        Assert.Equal(0, modalityGraduation.PromotionCount);

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
        var medal = await rankingRepository.CreateMedalAsync(new Medal
        {
            OrganizationId = organizationId,
            Code = "presenca-constante",
            Name = "Presença constante",
            PointsBonus = 5,
            IsActive = true
        });
        Assert.NotNull(medal);
        var medalAward = await rankingRepository.AwardMedalAsync(new PersonMedal
        {
            OrganizationId = organizationId,
            PersonId = authorizedPersonId,
            MedalId = medal.Id,
            Reason = "Participação exemplar"
        });
        Assert.NotNull(medalAward);
        Assert.Equal(medal.Id, Assert.Single(await rankingRepository.ListClientMedalsAsync(
            organizationId, authorizedClientId)).MedalId);

        var graduationSystem = await progressionRepository.CreateGraduationSystemAsync(new GraduationSystem
        {
            OrganizationId = organizationId,
            ModalityId = modalityId,
            Name = "Test graduation"
        });
        Assert.NotNull(graduationSystem);
        var whiteBelt = await progressionRepository.CreateGraduationBeltAsync(organizationId, graduationSystem.Id, new GraduationBelt
        {
            Name = "Branca",
            DegreeCount = 4
        });
        Assert.NotNull(whiteBelt);
        var firstDegree = await progressionRepository.CreateGraduationDegreeAsync(
            organizationId, graduationSystem.Id, whiteBelt.Id, new GraduationLevel
            {
                MinLessons = 1,
                MinTrainingDays = 1,
                RequiresManualEvaluation = true
            });
        Assert.NotNull(firstDegree);
        Assert.Equal("Branca - 1º grau", firstDegree.Name);
        for (var degreeNumber = 2; degreeNumber <= 4; degreeNumber++)
        {
            var degree = await progressionRepository.CreateGraduationDegreeAsync(
                organizationId, graduationSystem.Id, whiteBelt.Id, new GraduationLevel
                {
                    MinLessons = degreeNumber * 10
                });
            Assert.NotNull(degree);
            Assert.Equal(degreeNumber, degree.DegreeNumber);
        }

        Assert.Null(await progressionRepository.CreateGraduationDegreeAsync(
            organizationId, graduationSystem.Id, whiteBelt.Id, new GraduationLevel { MinLessons = 100 }));
        var blueBelt = await progressionRepository.CreateGraduationBeltAsync(organizationId, graduationSystem.Id, new GraduationBelt
        {
            Name = "Azul",
            DegreeCount = 4,
            MinLessons = 10
        });
        Assert.NotNull(blueBelt);
        var configuredBelts = await progressionRepository.ListGraduationBeltsAsync(organizationId, graduationSystem.Id);
        Assert.Equal(["Branca", "Azul"], configuredBelts.Select(belt => belt.Name));
        Assert.Equal(4, configuredBelts[0].DegreeCount);
        Assert.Empty(await progressionRepository.ListGraduationBeltsAsync(Guid.NewGuid(), graduationSystem.Id));
        Assert.Null(await progressionRepository.CreateGraduationBeltAsync(
            organizationId, graduationSystem.Id, new GraduationBelt
            {
                Name = "branca",
                DegreeCount = 4
            }));
        Assert.Null(await progressionRepository.CreateGraduationDegreeAsync(
            Guid.NewGuid(), graduationSystem.Id, whiteBelt.Id, new GraduationLevel { MinLessons = 1 }));
        Assert.True(await progressionRepository.UpdateGraduationBeltAsync(
            organizationId, graduationSystem.Id, new GraduationBelt
            {
                Id = whiteBelt.Id,
                Name = "Branca corrigida",
                DegreeCount = 4,
                MinLessons = 2,
                MinTrainingDays = 1,
                MinAttendancePercentage = 80,
                RequiresManualEvaluation = true
            }));
        Assert.False(await progressionRepository.UpdateGraduationBeltAsync(
            organizationId, graduationSystem.Id, new GraduationBelt
            {
                Id = whiteBelt.Id,
                Name = "Azul",
                DegreeCount = 4
            }));
        Assert.False(await progressionRepository.UpdateGraduationBeltAsync(
            organizationId, graduationSystem.Id, new GraduationBelt
            {
                Id = whiteBelt.Id,
                Name = "Branca corrigida",
                DegreeCount = 3
            }));
        Assert.True(await progressionRepository.UpdateGraduationDegreeAsync(
            organizationId, graduationSystem.Id, whiteBelt.Id, new GraduationLevel
            {
                Id = firstDegree.Id,
                MinLessons = 1,
                MinTrainingDays = 1,
                MinAttendancePercentage = 75,
                RequiresManualEvaluation = true
            }));
        var beltLevels = await progressionRepository.ListGraduationLevelsAsync(organizationId, graduationSystem.Id);
        Assert.Contains(beltLevels, level => level.Id == firstDegree.Id
            && level.Name == "Branca corrigida - 1º grau"
            && level.MinAttendancePercentage == 75);
        var whiteBeltLevel = Assert.Single(beltLevels, level => level.BeltId == whiteBelt.Id && level.DegreeNumber == 0);
        Assert.Equal("Branca corrigida", whiteBeltLevel.Name);

        var temporaryBelt = await progressionRepository.CreateGraduationBeltAsync(
            organizationId, graduationSystem.Id, new GraduationBelt { Name = "Amarela temporária", DegreeCount = 2 });
        Assert.NotNull(temporaryBelt);
        var temporaryFirstDegree = await progressionRepository.CreateGraduationDegreeAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id, new GraduationLevel());
        var temporaryLastDegree = await progressionRepository.CreateGraduationDegreeAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id, new GraduationLevel());
        Assert.NotNull(temporaryFirstDegree);
        Assert.NotNull(temporaryLastDegree);
        Assert.False(await progressionRepository.DeleteGraduationDegreeAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id, temporaryFirstDegree.Id));
        Assert.True(await progressionRepository.DeleteGraduationDegreeAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id, temporaryLastDegree.Id));
        Assert.True(await progressionRepository.DeleteGraduationDegreeAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id, temporaryFirstDegree.Id));
        Assert.True(await progressionRepository.DeleteGraduationBeltAsync(
            organizationId, graduationSystem.Id, temporaryBelt.Id));

        var initialGraduation = await progressionRepository.SetClientGraduationAsync(new ClientGraduation
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            ModalityId = modalityId,
            CurrentLevelId = whiteBeltLevel.Id,
            GrantedAt = new DateOnly(2026, 1, 4)
        });
        Assert.NotNull(initialGraduation);
        Assert.Null(await progressionRepository.SetClientGraduationAsync(new ClientGraduation
        {
            OrganizationId = organizationId,
            ClientId = unauthorizedClientId,
            ModalityId = modalityId,
            CurrentLevelId = whiteBeltLevel.Id,
            GrantedAt = new DateOnly(2026, 1, 4)
        }));
        var graduationSystemId = graduationSystem.Id;
        var currentLevelId = whiteBeltLevel.Id;
        var nextLevelId = firstDegree.Id;

        var fitnessModalityId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into modalities (id, organization_id, name, type, is_active)
                values (@Id, @OrganizationId, 'Condicionamento', 'fitness', true);
                """,
                connection);
            command.Parameters.AddWithValue("Id", fitnessModalityId);
            command.Parameters.AddWithValue("OrganizationId", organizationId);
            await command.ExecuteNonQueryAsync();
        }

        Assert.Null(await progressionRepository.CreateGraduationSystemAsync(new GraduationSystem
        {
            OrganizationId = organizationId,
            ModalityId = fitnessModalityId,
            Name = "Invalid fitness graduation"
        }));

        var eligibleForGraduation = await progressionRepository.ListEligibleClientsAsync(organizationId, graduationSystemId);
        var eligibleClient = Assert.Single(eligibleForGraduation);
        Assert.Equal(authorizedClientId, eligibleClient.ClientId);
        Assert.Equal(nextLevelId, eligibleClient.NextLevelId);

        var promotionRequest = new GraduationEvent
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            ModalityId = modalityId,
            ToLevelId = nextLevelId,
            EventDate = new DateOnly(2026, 1, 5),
            Justification = "Aprovado em avaliação"
        };
        Assert.Null(await progressionRepository.PromoteClientAsync(promotionRequest, manualEvaluationPassed: false));
        var promotion = await progressionRepository.PromoteClientAsync(promotionRequest, manualEvaluationPassed: true);
        Assert.NotNull(promotion);
        Assert.Equal(currentLevelId, promotion.FromLevelId);
        Assert.Equal(nextLevelId, promotion.ToLevelId);
        Assert.Empty(await progressionRepository.ListEligibleClientsAsync(organizationId, graduationSystemId));
        Assert.Null(await progressionRepository.PromoteClientAsync(new GraduationEvent
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            ModalityId = modalityId,
            ToLevelId = blueBelt.Id,
            EventDate = new DateOnly(2026, 1, 6)
        }, manualEvaluationPassed: true));
        var graduationEvents = await progressionRepository.ListClientGraduationEventsAsync(
            organizationId, authorizedClientId, modalityId);
        Assert.Equal(promotion.Id, Assert.Single(graduationEvents).Id);
        Assert.False(await progressionRepository.DeleteGraduationDegreeAsync(
            organizationId, graduationSystem.Id, whiteBelt.Id, firstDegree.Id));
        Assert.False(await progressionRepository.DeleteGraduationBeltAsync(
            organizationId, graduationSystem.Id, whiteBelt.Id));

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                select current_level_id = @nextLevelId and granted_at = @grantedAt
                from client_graduations
                where organization_id = @organizationId and client_id = @clientId and modality_id = @modalityId;
                """,
                connection);
            command.Parameters.AddWithValue("nextLevelId", nextLevelId);
            command.Parameters.AddWithValue("grantedAt", new DateOnly(2026, 1, 5));
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("clientId", authorizedClientId);
            command.Parameters.AddWithValue("modalityId", modalityId);
            Assert.True((bool)(await command.ExecuteScalarAsync())!);
        }

        var fitnessGoal = await progressionRepository.CreateFitnessGoalAsync(new FitnessGoal
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            ModalityId = modalityId,
            Title = "Treinar 12 dias",
            GoalType = "training_days",
            TargetValue = 12,
            Unit = "dias",
            StartsAt = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 2, 1)
        });
        Assert.NotNull(fitnessGoal);
        var progress = await progressionRepository.RecordFitnessProgressAsync(organizationId, new FitnessProgressRecord
        {
            FitnessGoalId = fitnessGoal.Id,
            RecordedValue = 12,
            Notes = "Meta atingida",
            CreatedByPersonId = teacherPersonId
        });
        Assert.NotNull(progress);
        Assert.Equal(12m, progress.RecordedValue);
        Assert.Equal(teacherId, progress.CreatedByTeacherId);
        var progressHistory = await progressionRepository.ListFitnessProgressRecordsAsync(
            organizationId, fitnessGoal.Id, page: 1, pageSize: 20);
        Assert.Equal(progress.Id, Assert.Single(progressHistory).Id);
        Assert.Empty(await progressionRepository.ListFitnessProgressRecordsAsync(
            Guid.NewGuid(), fitnessGoal.Id, page: 1, pageSize: 20));
        var achievedGoal = Assert.Single(await progressionRepository.ListFitnessGoalsAsync(
            organizationId, authorizedClientId, 1, 20));
        Assert.Equal("achieved", achievedGoal.Status);
        Assert.False(await progressionRepository.CancelFitnessGoalAsync(organizationId, fitnessGoal.Id));

        var goalWithHistory = await progressionRepository.CreateFitnessGoalAsync(new FitnessGoal
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            Title = "Acompanhar evolução",
            GoalType = "custom",
            TargetValue = 10,
            StartsAt = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 2, 1)
        });
        Assert.NotNull(goalWithHistory);
        var clientRecordedProgress = await progressionRepository.RecordFitnessProgressAsync(organizationId, new FitnessProgressRecord
        {
            FitnessGoalId = goalWithHistory.Id,
            RecordedValue = 2,
            CreatedByPersonId = authorizedPersonId
        });
        Assert.NotNull(clientRecordedProgress);
        Assert.Null(clientRecordedProgress.CreatedByTeacherId);
        await progressionRepository.RecordFitnessProgressAsync(organizationId, new FitnessProgressRecord
        {
            FitnessGoalId = goalWithHistory.Id,
            RecordedValue = 4
        });
        var firstHistoryPage = await progressionRepository.ListFitnessProgressRecordsAsync(
            organizationId, goalWithHistory.Id, page: 1, pageSize: 1);
        var secondHistoryPage = await progressionRepository.ListFitnessProgressRecordsAsync(
            organizationId, goalWithHistory.Id, page: 2, pageSize: 1);
        Assert.Single(firstHistoryPage);
        Assert.Single(secondHistoryPage);
        Assert.NotEqual(firstHistoryPage[0].Id, secondHistoryPage[0].Id);

        var cancellableGoal = await progressionRepository.CreateFitnessGoalAsync(new FitnessGoal
        {
            OrganizationId = organizationId,
            ClientId = authorizedClientId,
            Title = "Meta temporária",
            GoalType = "custom",
            TargetValue = 5,
            StartsAt = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 2, 1)
        });
        Assert.NotNull(cancellableGoal);
        Assert.True(await progressionRepository.CancelFitnessGoalAsync(organizationId, cancellableGoal.Id));
        Assert.False(await progressionRepository.CancelFitnessGoalAsync(organizationId, cancellableGoal.Id));

        var notification = await notificationRepository.CreateAsync(new Notification
        {
            OrganizationId = organizationId,
            PersonId = authorizedPersonId,
            Title = "Aviso de aula",
            Body = "A aula começa às 18h.",
            Channel = "in_app"
        });
        Assert.NotNull(notification);
        Assert.Equal("sent", notification.Status);
        var outbox = Assert.Single(await notificationRepository.ListByOrganizationAsync(organizationId, 1, 20));
        Assert.Equal(notification.Id, outbox.Id);
        Assert.Equal(authorizedPersonId, outbox.PersonId);
        var inbox = Assert.Single(await notificationRepository.ListByPersonAsync(organizationId, authorizedPersonId, 1, 20));
        Assert.Equal(notification.Id, inbox.Id);
        Assert.True(await notificationRepository.MarkAsReadAsync(organizationId, authorizedPersonId, notification.Id));
        Assert.NotNull(Assert.Single(await notificationRepository.ListByPersonAsync(
            organizationId, authorizedPersonId, 1, 20)).ReadAt);
        Assert.False(await notificationRepository.MarkAsReadAsync(organizationId, unauthorizedPersonId, notification.Id));

        var auditFrom = DateTimeOffset.UtcNow.AddMinutes(-1);
        await auditLogRepository.RecordAsync(new AuditLog
        {
            OrganizationId = organizationId,
            ActorPersonId = teacherPersonId,
            Action = "POST",
            EntityName = "lessons",
            EntityId = firstGeneration[0].Id,
            Metadata = """{"method":"POST","statusCode":201}""",
            IpAddress = "127.0.0.1",
            UserAgent = "integration-test"
        });
        var auditLogs = await auditLogRepository.ListAsync(
            organizationId, auditFrom, DateTimeOffset.UtcNow.AddMinutes(1), 1, 20);
        var auditLog = Assert.Single(auditLogs);
        Assert.Equal("POST", auditLog.Action);
        Assert.Equal(firstGeneration[0].Id, auditLog.EntityId);
        Assert.Empty(await auditLogRepository.ListAsync(Guid.NewGuid(), auditFrom, null, 1, 20));

        Assert.True(await scheduleRepository.DeactivateAsync(organizationId, modalityId, schedule.Id));
        Assert.Empty(await scheduleRepository.ListAsync(organizationId, modalityId));
        Assert.NotNull(await lessonRepository.GetByIdAsync(organizationId, firstGeneration[0].Id));
    }
}
