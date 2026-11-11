using Dapper;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.UseCases.Catalog;
using KiaiControl.UseCases.Fitness;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class WorkspaceAreaCatalogTests(PostgreSqlFixture fixture)
{
    private ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> OrganizationAsync(NpgsqlConnection c)
    {
        var id = Guid.NewGuid();
        await c.ExecuteAsync("insert into organizations(id,name) values(@id,'Area catalog')", new { id });
        return id;
    }

    private static async Task<Guid> ModalityAsync(NpgsqlConnection c, Guid org, string type)
    {
        var id = Guid.NewGuid();
        await c.ExecuteAsync("insert into modalities(id,organization_id,name,type) values(@id,@org,@name,cast(@type as modality_type))", new { id, org, type, name = $"{type}-{id}" });
        return id;
    }

    private static async Task<Guid> StudentAsync(NpgsqlConnection c, Guid org, string name, params Guid[] modalities)
    {
        var id = Guid.NewGuid();
        var person = Guid.NewGuid();
        await c.ExecuteAsync("insert into persons(id,organization_id,full_name) values(@person,@org,@name); insert into clients(id,organization_id,person_id) values(@id,@org,@person)", new { id, person, org, name });
        foreach (var modality in modalities)
            await c.ExecuteAsync("insert into person_modalities(person_id,modality_id) values(@person,@modality)", new { person, modality });
        return id;
    }

    [Fact]
    public async Task Students_ShouldFilterBeforePaginationAndCountDistinctAcrossAreasAndTenants()
    {
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        var org = await OrganizationAsync(c);
        var otherOrg = await OrganizationAsync(c);
        var fight = await ModalityAsync(c, org, "martial_art");
        var secondFight = await ModalityAsync(c, org, "martial_art");
        var gym = await ModalityAsync(c, org, "fitness");
        var hybrid = await ModalityAsync(c, org, "hybrid");
        var foreignFight = await ModalityAsync(c, otherOrg, "martial_art");
        for (var i = 1; i <= 30; i++)
            await StudentAsync(c, org, $"Aluno Fight {i:00}", fight, secondFight);
        var onlyGym = await StudentAsync(c, org, "Aluno Gym", gym);
        var both = await StudentAsync(c, org, "Aluno Duas", fight, gym);
        var hybridStudent = await StudentAsync(c, org, "Aluno Híbrido", hybrid);
        var unlinked = await StudentAsync(c, org, "Aluno Sem Modalidade");
        var wrongTenantLink = await StudentAsync(c, org, "Vínculo de outro tenant", foreignFight);
        await StudentAsync(c, otherOrg, "Aluno de outra academia", foreignFight);
        using var services = Provider();
        var catalog = new CatalogUseCase(services.GetRequiredService<ICatalogRepository>());

        var first = await catalog.ClientsAsync(org, 1, 25, null, default, "fight");
        var second = await catalog.ClientsAsync(org, 2, 25, null, default, "fight");
        Assert.Equal(32, first.Total);
        Assert.Equal(32, second.Total);
        Assert.Equal(25, first.Items.Count);
        Assert.Equal(7, second.Items.Count);
        Assert.Equal(32, first.Items.Concat(second.Items).Select(x => x.Id).Distinct().Count());
        var gymView = await catalog.ClientsAsync(org, 1, 25, null, default, "gym");
        Assert.Equal(3, gymView.Total);
        Assert.Equal(new[] { both, hybridStudent, onlyGym }.Order(), gymView.Items.Select(x => x.Id).Order());
        Assert.Equal(new[] { "fight", "gym" }, gymView.Items.Single(x => x.Id == both).Areas);
        Assert.Equal(new[] { "fight", "gym" }, gymView.Items.Single(x => x.Id == hybridStudent).Areas);
        var general = await catalog.ClientsAsync(org, 1, 200, null, default);
        Assert.Equal(35, general.Total);
        Assert.Empty(general.Items.Single(x => x.Id == unlinked).Areas);
        Assert.Empty(general.Items.Single(x => x.Id == wrongTenantLink).Areas);
        Assert.Equal(30, (await catalog.ClientsAsync(org, 1, 200, "Fight", default, "fight")).Total);
        Assert.Empty((await catalog.ClientsAsync(org, 1, 25, "Fight", default, "gym")).Items);
        Assert.Equal(1, (await catalog.ClientsAsync(otherOrg, 1, 25, null, default, "fight")).Total);
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.ClientsAsync(org, 1, 25, null, default, "invalid"));

        var fitness = new FitnessUseCase(services.GetRequiredService<IProgressionRepository>());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var client in new[] { onlyGym, both, hybridStudent, unlinked, wrongTenantLink })
            await fitness.CreateAsync(org, new() { ClientId = client, Title = "Meta por área", GoalType = "weight", TargetValue = 70, StartsAt = today, DueDate = today.AddDays(30) }, default);
        Assert.Equal(5, (await fitness.ListAsync(org, null, 1, 25, default)).Count);
        Assert.Equal(2, (await fitness.ListAsync(org, null, 1, 25, default, "fight")).Count);
        Assert.Equal(3, (await fitness.ListAsync(org, null, 1, 25, default, "gym")).Count);
        var goalFirst = Assert.Single(await fitness.ListAsync(org, null, 1, 1, default, "gym"));
        var goalSecond = Assert.Single(await fitness.ListAsync(org, null, 2, 1, default, "gym"));
        Assert.NotEqual(goalFirst.Id, goalSecond.Id);
        Assert.Empty(await fitness.ListAsync(org, wrongTenantLink, 1, 25, default, "fight"));
        Assert.Empty(await fitness.ListAsync(otherOrg, null, 1, 25, default, "gym"));
        var martialGoal = await fitness.CreateAsync(org, new() { ClientId = both, ModalityId = fight, Title = "Meta da modalidade Fight", GoalType = "weight", TargetValue = 70, StartsAt = today, DueDate = today.AddDays(30) }, default);
        Assert.NotNull(martialGoal);
        Assert.Contains(await fitness.ListAsync(org, null, 1, 25, default, "fight"), x => x.Id == martialGoal.Id);
        Assert.DoesNotContain(await fitness.ListAsync(org, null, 1, 25, default, "gym"), x => x.Id == martialGoal.Id);

        await c.ExecuteAsync("delete from person_modalities where person_id=(select person_id from clients where id=@both) and modality_id=@gym", new { both, gym });
        Assert.Equal(2, (await catalog.ClientsAsync(org, 1, 25, null, default, "gym")).Total);
        Assert.Equal(32, (await catalog.ClientsAsync(org, 1, 25, null, default, "fight")).Total);
    }

    [Fact]
    public async Task Catalogs_ShouldApplyAreaWithoutBroadeningTheTeacherLessonScope()
    {
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        var org = await OrganizationAsync(c);
        var otherOrg = await OrganizationAsync(c);
        var fight = await ModalityAsync(c, org, "martial_art");
        var gym = await ModalityAsync(c, org, "fitness");
        var hybrid = await ModalityAsync(c, org, "hybrid");
        var person = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        await c.ExecuteAsync("insert into persons(id,organization_id,full_name) values(@person,@org,'Professor Fight');insert into teachers(id,organization_id,person_id) values(@teacher,@org,@person)", new { person, teacher, org });
        foreach (var modality in new[] { fight, gym, hybrid })
        {
            var lesson = Guid.NewGuid();
            await c.ExecuteAsync("insert into lessons(id,organization_id,modality_id,starts_at,ends_at) values(@lesson,@org,@modality,now(),now()+interval '1 hour')", new { lesson, org, modality });
            if (modality != gym)
                await c.ExecuteAsync("insert into lesson_instructors(id,lesson_id,teacher_id) values(gen_random_uuid(),@lesson,@teacher)", new { lesson, teacher });
        }
        using var services = Provider();
        var catalog = new CatalogUseCase(services.GetRequiredService<ICatalogRepository>());
        Assert.Equal(3, (await catalog.ModalitiesAsync(org, 1, 25, null, default)).Total);
        Assert.Equal(2, (await catalog.ModalitiesAsync(org, 1, 25, null, default, "fight")).Total);
        Assert.Equal(2, (await catalog.ModalitiesAsync(org, 1, 25, null, default, "gym")).Total);
        Assert.Equal(2, (await catalog.LessonsAsync(org, 1, 25, null, null, null, null, default, "gym")).Total);
        Assert.Equal(2, (await catalog.LessonsAsync(org, 1, 25, null, null, null, teacher, default, "fight")).Total);
        Assert.Equal(hybrid, Assert.Single((await catalog.LessonsAsync(org, 1, 25, null, null, null, teacher, default, "gym")).Items).ModalityId);
        Assert.Single((await catalog.TeachersAsync(org, 1, 25, null, default, "fight")).Items);
        Assert.Single((await catalog.TeachersAsync(org, 1, 25, null, default, "gym")).Items);
        Assert.Empty((await catalog.LessonsAsync(otherOrg, 1, 25, null, null, null, teacher, default, "fight")).Items);
        Assert.Empty((await catalog.TeachersAsync(otherOrg, 1, 25, null, default, "fight")).Items);
    }
}
