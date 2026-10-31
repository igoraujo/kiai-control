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
            if (Path.GetFileName(migrationPath) == "007_remove_class_groups.sql")
            {
                await SeedClassGroupRecordsAsync(connection);
            }

            var migration = await File.ReadAllTextAsync(migrationPath);
            await using var command = new NpgsqlCommand(migration, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private static async Task SeedClassGroupRecordsAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into organizations (id, name, status)
            values ('00000000-0000-0000-0000-000000000701', 'Migration conversion test', 'active');
            insert into modalities (id, organization_id, name, type, is_active)
            values ('00000000-0000-0000-0000-000000000702', '00000000-0000-0000-0000-000000000701', 'Migration modality', 'martial_art', true);
            insert into persons (id, organization_id, full_name)
            values ('00000000-0000-0000-0000-000000000703', '00000000-0000-0000-0000-000000000701', 'Migration student');
            insert into clients (id, organization_id, person_id, current_status)
            values ('00000000-0000-0000-0000-000000000704', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000703', 'active');
            insert into class_groups (id, organization_id, modality_id, name, capacity, status)
            values ('00000000-0000-0000-0000-000000000705', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000702', 'Migration group', 10, 'active');
            insert into enrollments (id, organization_id, class_group_id, client_id, enrolled_at, status)
            values ('00000000-0000-0000-0000-000000000709', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000705', '00000000-0000-0000-0000-000000000704', '2025-01-02T10:00:00Z', 'active');
            insert into lessons (id, organization_id, class_group_id, starts_at, ends_at, status)
            values ('00000000-0000-0000-0000-000000000706', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000705', '2026-01-05T21:00:00Z', '2026-01-05T22:00:00Z', 'completed');
            insert into attendances (id, organization_id, lesson_id, client_id, status)
            values ('00000000-0000-0000-0000-000000000707', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000706', '00000000-0000-0000-0000-000000000704', 'present');
            insert into ranking_cycles (id, organization_id, class_group_id, name, starts_at, ends_at, status)
            values ('00000000-0000-0000-0000-000000000708', '00000000-0000-0000-0000-000000000701', '00000000-0000-0000-0000-000000000705', 'Migration cycle', '2026-01-01', '2026-01-31', 'open');
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

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