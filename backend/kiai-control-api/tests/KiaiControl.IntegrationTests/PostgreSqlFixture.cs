using Npgsql;
using Testcontainers.PostgreSql;

namespace KiaiControl.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("kiai_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var migrationsPath = Path.Combine(FindRepositoryRoot(), "database", "kiai-control-database", "migrations");
        foreach (var migrationPath in Directory.GetFiles(migrationsPath, "*.sql").Order(StringComparer.Ordinal))
        {
            var migration = await File.ReadAllTextAsync(migrationPath);
            await using var command = new NpgsqlCommand(migration, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var migrationPath = Path.Combine(directory.FullName, "database", "kiai-control-database", "migrations", "001_initial_api_schema.sql");
            if (File.Exists(migrationPath))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository migrations directory.");
    }
}