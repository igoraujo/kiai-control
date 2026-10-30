using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[CollectionDefinition("PostgreSQL integration tests", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}

[Collection(PostgreSqlCollection.Name)]
public sealed class SqlClientRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CreateAndGetByIdAsync_ShouldRoundTripDateOnlyValues()
    {
        var organizationId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "insert into organizations (id, name, status) values (@id, @name, 'active')",
                connection);
            command.Parameters.AddWithValue("id", organizationId);
            command.Parameters.AddWithValue("name", "Integration Test Academy");
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IClientRepository>();
        var expectedBirthDate = new DateOnly(1990, 3, 10);
        var expectedJoinedAt = new DateOnly(2020, 9, 1);

        var created = await repository.CreateAsync(new Client
        {
            OrganizationId = organizationId,
            FullName = "Ana Silva",
            BirthDate = expectedBirthDate,
            JoinedAt = expectedJoinedAt,
            Status = "active"
        });
        var retrieved = await repository.GetByIdAsync(organizationId, created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(expectedBirthDate, created.BirthDate);
        Assert.Equal(expectedJoinedAt, created.JoinedAt);
        Assert.Equal(expectedBirthDate, retrieved.BirthDate);
        Assert.Equal(expectedJoinedAt, retrieved.JoinedAt);
    }
}