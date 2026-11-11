using Dapper;
using KiaiControl.Core.Interfaces;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.UseCases.Fitness;
using KiaiControl.UseCases.Catalog;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;
[Collection(PostgreSqlCollection.Name)]
public sealed class AdminCompletionRepositoryTests(PostgreSqlFixture fixture)
{
    private ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private async Task<Guid> OrganizationAsync()
    {
        var id = Guid.NewGuid();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("insert into organizations(id,name) values(@id,'Admin completion'); insert into organization_settings(id,organization_id) values(gen_random_uuid(),@id)", new { id });
        return id;
    }

    [Fact]
    public async Task Settings_ShouldReadDefaultsAndSaveForLegacyOrganizationWithoutSettings()
    {
        var org = Guid.NewGuid();
        var otherOrg = await OrganizationAsync();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("insert into organizations(id,name) values(@org,'Legacy academy')", new { org });
        using var services = Provider();
        var administration = services.GetRequiredService<IAdministrationRepository>();

        var settings = await administration.GetSettingsAsync(org, default);
        Assert.Equal("America/Sao_Paulo", settings.Timezone);
        Assert.Equal("BRL", settings.CurrencyCode);
        Assert.False(settings.AllowClientCheckin);
        Assert.True(settings.RequireTeacherConfirmation);
        Assert.Equal(10, settings.DefaultLateToleranceMinutes);

        settings.AllowClientCheckin = true;
        settings.Timezone = "UTC";
        await administration.SaveSettingsAsync(org, settings, default);
        Assert.True((await administration.GetSettingsAsync(org, default)).AllowClientCheckin);
        Assert.Equal("UTC", (await administration.GetSettingsAsync(org, default)).Timezone);

        settings.RequireTeacherConfirmation = false;
        await administration.SaveSettingsAsync(org, settings, default);
        Assert.False((await administration.GetSettingsAsync(org, default)).RequireTeacherConfirmation);
        Assert.False((await administration.GetSettingsAsync(otherOrg, default)).AllowClientCheckin);
        Assert.Equal(1, await c.QuerySingleAsync<int>("select count(1)::int from organization_settings where organization_id=@org", new { org }));
    }

    [Fact]
    public async Task DirectoryProfilesAndAccountManagement_ShouldPreserveIdentityAndRejectCrossTenantAndSelfChanges()
    {
        var org = await OrganizationAsync();
        var otherOrg = await OrganizationAsync();
        using var services = Provider();
        var people = services.GetRequiredService<IAdministrationRepository>();
        var access = services.GetRequiredService<IAccessControlRepository>();
        var person = await people.SavePersonAsync(org, null, "Equipe compartilhada", $"{Guid.NewGuid()}@example.test", null, default);
        var actor = await people.SavePersonAsync(org, null, "Responsável", $"{Guid.NewGuid()}@example.test", null, default);
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        var actorUser = Guid.NewGuid();
        await c.ExecuteAsync("insert into user_accounts(id,person_id,email,password_hash) values(@actorUser,@actor,@email,'hash')", new { actorUser, actor, email = $"{Guid.NewGuid()}@example.test" });
        Assert.False(await people.AddProfileAsync(otherOrg, person, "client", actorUser, default));
        Assert.True(await people.AddProfileAsync(org, person, "client", actorUser, default));
        Assert.True(await people.AddProfileAsync(org, person, "teacher", actorUser, default));
        Assert.True(await people.AddProfileAsync(org, person, "teacher", actorUser, default));
        var result = await people.ListPeopleAsync(org, 1, 25, "compartilhada", default);
        var entry = Assert.Single(result.Items);
        Assert.Equal(person, entry.PersonId);
        Assert.NotNull(entry.ClientId);
        Assert.NotNull(entry.TeacherId);
        Assert.Equal(2, entry.Roles.Length);
        Assert.True(await access.CreateUserAccountAsync(org, person, $"{Guid.NewGuid()}@example.test", "hash", "client", actorUser));
        Assert.False(await people.ManageAccountAsync(otherOrg, person, actorUser, true, false, null, default));
        Assert.False(await people.ManageAccountAsync(org, actor, actorUser, true, false, null, default));
        Assert.True(await people.ManageAccountAsync(org, person, actorUser, true, false, null, default));
        var account = await c.QuerySingleAsync<(bool Active, int Version)>("select is_active,session_version from user_accounts where person_id=@person", new { person });
        Assert.False(account.Active);
        Assert.Equal(1, account.Version);
        Assert.True(await people.ManageAccountAsync(org, person, actorUser, true, true, "new-hash", default));
        Assert.True(await c.QuerySingleAsync<bool>("select must_change_password from user_accounts where person_id=@person", new { person }));
        var updatedEmail = $"updated-{Guid.NewGuid()}@example.test";
        Assert.True(await people.ManageAccountAsync(org, person, actorUser, true, null, null, default, updatedEmail));
        Assert.Equal(updatedEmail, await c.QuerySingleAsync<string>("select email from user_accounts where person_id=@person", new { person }));
    }

    [Fact]
    public async Task Catalog_ShouldSearchBeyondFirstHundredAndKeepCountsWithinTenant()
    {
        var org = await OrganizationAsync();
        using var services = Provider();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("""
            with created as (
                insert into persons(id,organization_id,full_name)
                select gen_random_uuid(),@org,'Aluno catálogo '||lpad(n::text,3,'0') from generate_series(1,125) n returning id
            ) insert into clients(id,organization_id,person_id) select gen_random_uuid(),@org,id from created;
            """, new { org });
        var catalog = services.GetRequiredService<ICatalogRepository>();
        var first = await catalog.ClientsAsync(org, 1, 25, null, default);
        var last = await catalog.ClientsAsync(org, 5, 25, null, default);
        Assert.Equal(125, first.Total);
        Assert.Equal(25, first.Items.Count);
        Assert.Equal(25, last.Items.Count);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(last.Items.Select(x => x.Id)));
        Assert.Single((await catalog.ClientsAsync(org, 1, 25, "125", default)).Items);
        Assert.Empty((await catalog.ClientsAsync(Guid.NewGuid(), 1, 25, "125", default)).Items);
        Assert.Empty((await catalog.ClientsAsync(org, 1, 25, "%", default)).Items);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(9)]
    public async Task LessonCatalog_ShouldFilterLocalDateRangesAndPaginateWithinTenant(int offsetHours)
    {
        var org = await OrganizationAsync();
        var otherOrg = await OrganizationAsync();
        var modality = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.FromHours(offsetHours));
        var to = from.AddDays(1);
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("insert into modalities(id,organization_id,name,type) values(@modality,@org,'Timezone lessons','martial_art')", new { modality, org });
        foreach (var (startsAt, status) in new[]
        {
            (from.AddHours(-1), "scheduled"),
            (from, "scheduled"),
            (from.AddHours(12), "completed"),
            (to, "scheduled")
        })
        {
            await c.ExecuteAsync("""
                insert into lessons(id,organization_id,modality_id,starts_at,ends_at,status)
                values(gen_random_uuid(),@org,@modality,@startsAt,@endsAt,cast(@status as lesson_status))
                """, new { org, modality, startsAt = startsAt.ToUniversalTime(), endsAt = startsAt.AddHours(1).ToUniversalTime(), status });
        }

        using var services = Provider();
        var catalog = new CatalogUseCase(services.GetRequiredService<ICatalogRepository>());
        var first = await catalog.LessonsAsync(org, 1, 1, from, to, null, null, default);
        var second = await catalog.LessonsAsync(org, 2, 1, from, to, null, null, default);
        Assert.Equal(2, first.Total);
        Assert.Equal(2, second.Total);
        Assert.Equal(from.AddHours(12), Assert.Single(first.Items).StartsAt);
        Assert.Equal(from, Assert.Single(second.Items).StartsAt);
        Assert.Equal(1, (await catalog.LessonsAsync(org, 1, 25, from, to, "completed", null, default)).Total);
        Assert.Equal(4, (await catalog.LessonsAsync(org, 1, 25, null, null, null, null, default)).Total);
        Assert.Equal(3, (await catalog.LessonsAsync(org, 1, 25, from, null, null, null, default)).Total);
        Assert.Equal(3, (await catalog.LessonsAsync(org, 1, 25, null, to, null, null, default)).Total);
        Assert.Empty((await catalog.LessonsAsync(otherOrg, 1, 25, from, to, null, null, default)).Items);
    }

    [Fact]
    public async Task StudentJourneyAndReducingFitnessGoal_ShouldReturnOnlyOwnedDataAndUseGoalDirection()
    {
        var org = await OrganizationAsync();
        using var services = Provider();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        var person = Guid.NewGuid();
        var client = Guid.NewGuid();
        await c.ExecuteAsync("insert into persons(id,organization_id,full_name) values(@person,@org,'Aluno evolução');insert into clients(id,organization_id,person_id) values(@client,@org,@person)", new { org, person, client });
        var fitness = new FitnessUseCase(services.GetRequiredService<IProgressionRepository>());
        var goal = await fitness.CreateAsync(org, new CreateFitnessGoalRequest { ClientId = client, Title = "Redução de peso", GoalType = "weight", Direction = "decrease", InitialValue = 80, TargetValue = 70, Unit = "kg", StartsAt = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) }, default);
        Assert.NotNull(goal);
        Assert.Equal(80, goal.CurrentValue);
        await fitness.RecordProgressAsync(org, goal.Id, null, new() { RecordedValue = 79 }, default);
        Assert.Equal("active", Assert.Single(await fitness.ListAsync(org, client, 1, 20, default)).Status);
        await fitness.RecordProgressAsync(org, goal.Id, null, new() { RecordedValue = 70 }, default);
        Assert.Equal("achieved", Assert.Single(await fitness.ListAsync(org, client, 1, 20, default)).Status);
        var journey = services.GetRequiredService<IClientJourneyRepository>();
        Assert.NotNull(await journey.GetAsync(org, client, 1, 20, default));
        Assert.Null(await journey.GetAsync(Guid.NewGuid(), client, 1, 20, default));
        Assert.Equal(client, await journey.GetGoalClientIdAsync(org, goal.Id, default));
        Assert.Null(await journey.GetGoalClientIdAsync(Guid.NewGuid(), goal.Id, default));
    }
}
