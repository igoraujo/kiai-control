using Dapper;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ProductionSchemaTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ConsolidatedSchemaAndSeed_ShouldCreateFreshDatabaseAndAllowReapplication()
    {
        // The identifier is generated locally; no application database is selected or dropped.
        var database = "kiai_snapshot_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(fixture.ConnectionString);
        await admin.OpenAsync();
        await admin.ExecuteAsync($"create database {database}");
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
            {
                Database = database,
                Pooling = false
            };
            await using var snapshot = new NpgsqlConnection(builder.ConnectionString);
            await snapshot.OpenAsync();
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SYSTEM_DESIGN.md")))
                directory = directory.Parent;
            Assert.NotNull(directory);
            var scripts = Path.Combine(directory.FullName, "database", "kiai-control-database", "scripts", "start_production");
            for (var iteration = 0; iteration < 2; iteration++)
            {
                await snapshot.ExecuteAsync(await File.ReadAllTextAsync(Path.Combine(scripts, "0001_create_production_schema.sql")));
                await snapshot.ExecuteAsync(await File.ReadAllTextAsync(Path.Combine(scripts, "0002_seed_production_reference_data.sql")));
            }
            Assert.Equal(39, await snapshot.QuerySingleAsync<int>("select count(1) from pg_tables where schemaname='public'"));
            Assert.Equal(4, await snapshot.QuerySingleAsync<int>("select count(1) from permissions where code in ('clients.read','teachers.read','branches.read','modalities.read')"));
            Assert.Equal(3, await snapshot.QuerySingleAsync<int>("""
                select count(1) from information_schema.columns where table_schema='public'
                  and ((table_name='user_accounts' and column_name='session_version')
                    or (table_name='fitness_goals' and column_name in ('direction','initial_value')))
                """));
        }
        finally
        {
            await admin.ExecuteAsync($"drop database {database} with (force)");
        }
    }
}
