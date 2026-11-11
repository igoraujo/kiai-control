using Dapper;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ParameterizedCatalogSearchTests(PostgreSqlFixture fixture)
{
    private const string Seed = @"
        insert into organizations (id, name)
        values (@OrganizationId, 'Parameterized search'), (@OtherOrganizationId, 'Other academy');

        insert into persons (id, organization_id, full_name)
        values
            (gen_random_uuid(), @OrganizationId, @Name),
            (gen_random_uuid(), @OtherOrganizationId, @Name),
            (gen_random_uuid(), @OrganizationId, 'Outra pessoa');

        insert into clients (id, organization_id, person_id)
        select gen_random_uuid(), organization_id, id
        from persons
        where organization_id = any(@OrganizationIds);

        insert into teachers (id, organization_id, person_id)
        select gen_random_uuid(), organization_id, id
        from persons
        where organization_id = any(@OrganizationIds);

        insert into branches (id, organization_id, name)
        values
            (gen_random_uuid(), @OrganizationId, @Name),
            (gen_random_uuid(), @OtherOrganizationId, @Name),
            (gen_random_uuid(), @OrganizationId, 'Outra unidade');

        insert into modalities (id, organization_id, name, type)
        values
            (gen_random_uuid(), @OrganizationId, @Name, 'fitness'),
            (gen_random_uuid(), @OtherOrganizationId, @Name, 'fitness'),
            (gen_random_uuid(), @OrganizationId, 'Outra modalidade', 'fitness');
        ";

    [Theory]
    [InlineData("D'Ávila")]
    [InlineData("%_\\")]
    [InlineData("x' OR 1=1 --")]
    [InlineData("x'); DROP TABLE persons; --")]
    [InlineData("x' UNION SELECT NULL --")]
    public async Task Searches_ShouldTreatSqlSyntaxAndWildcardsAsLiteralDataWithinTenant(string search)
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.ExecuteAsync(Seed, new
        {
            OrganizationId = organizationId,
            OtherOrganizationId = otherOrganizationId,
            OrganizationIds = new[] { organizationId, otherOrganizationId },
            Name = search
        });

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ICatalogRepository>();
        var administration = provider.GetRequiredService<IAdministrationRepository>();

        var clients = await catalog.ClientsAsync(organizationId, 1, 25, search, default);
        Assert.Equal(1, clients.Total);
        Assert.Equal(search, Assert.Single(clients.Items).FullName);
        var teachers = await catalog.TeachersAsync(organizationId, 1, 25, search, default);
        Assert.Equal(1, teachers.Total);
        Assert.Equal(search, Assert.Single(teachers.Items).FullName);
        var branches = await catalog.BranchesAsync(organizationId, 1, 25, search, default);
        Assert.Equal(1, branches.Total);
        Assert.Equal(search, Assert.Single(branches.Items).Name);
        var modalities = await catalog.ModalitiesAsync(organizationId, 1, 25, search, default);
        Assert.Equal(1, modalities.Total);
        Assert.Equal(search, Assert.Single(modalities.Items).Name);
        var people = await administration.ListPeopleAsync(organizationId, 1, 25, search, default);
        Assert.Equal(1, people.Total);
        Assert.Equal(search, Assert.Single(people.Items).FullName);

        Assert.Equal(2, (await catalog.ClientsAsync(organizationId, 1, 25, null, default)).Total);
        Assert.Equal(1, (await catalog.ClientsAsync(otherOrganizationId, 1, 25, search, default)).Total);
    }
}
