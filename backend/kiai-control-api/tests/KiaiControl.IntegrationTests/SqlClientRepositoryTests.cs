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

    [Fact]
    public async Task AuthUserPermissions_ShouldApplyOrganizationOverridesWithoutCrossTenantLeakage()
    {
        var firstOrganizationId = Guid.NewGuid();
        var secondOrganizationId = Guid.NewGuid();
        var firstUserId = await CreateTeacherUserAsync(firstOrganizationId, "teacher-one@example.test");
        var secondUserId = await CreateTeacherUserAsync(secondOrganizationId, "teacher-two@example.test");

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                insert into organization_role_permissions (
                    organization_id, role_id, permission_id, is_granted, changed_at, changed_by_user_account_id
                )
                select @OrganizationId, role.id, permission.id, permission.code = 'reports.read', now(), @ActorUserId
                from roles role
                cross join permissions permission
                where role.code = 'teacher'
                  and permission.code in ('lessons.manage', 'reports.read')
                on conflict (organization_id, role_id, permission_id) do update
                set is_granted = excluded.is_granted,
                    changed_at = excluded.changed_at,
                    changed_by_user_account_id = excluded.changed_by_user_account_id;
                """,
                connection);
            command.Parameters.AddWithValue("OrganizationId", firstOrganizationId);
            command.Parameters.AddWithValue("ActorUserId", firstUserId);
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IAuthRepository>();
        var firstUser = await repository.GetByIdAsync(firstUserId);
        var secondUser = await repository.GetByIdAsync(secondUserId);

        Assert.NotNull(firstUser);
        Assert.NotNull(secondUser);
        Assert.DoesNotContain("lessons.manage", firstUser.Permissions);
        Assert.Contains("reports.read", firstUser.Permissions);
        Assert.Contains("lessons.manage", secondUser.Permissions);
        Assert.DoesNotContain("reports.read", secondUser.Permissions);
    }

    private async Task<Guid> CreateTeacherUserAsync(Guid organizationId, string email)
    {
        var userId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using (var command = new NpgsqlCommand(
            "insert into organizations (id, name, status) values (@id, @name, 'active')",
            connection))
        {
            command.Parameters.AddWithValue("id", organizationId);
            command.Parameters.AddWithValue("name", $"Academy {organizationId:N}");
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand(
            "insert into persons (id, organization_id, full_name, email) values (@id, @organizationId, @name, @email)",
            connection))
        {
            command.Parameters.AddWithValue("id", personId);
            command.Parameters.AddWithValue("organizationId", organizationId);
            command.Parameters.AddWithValue("name", email);
            command.Parameters.AddWithValue("email", email);
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand(
            "insert into user_accounts (id, person_id, email, password_hash) values (@id, @personId, @email, 'test-hash')",
            connection))
        {
            command.Parameters.AddWithValue("id", userId);
            command.Parameters.AddWithValue("personId", personId);
            command.Parameters.AddWithValue("email", email);
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand(
            "insert into person_roles (person_id, role_id) select @personId, id from roles where code = 'teacher'",
            connection))
        {
            command.Parameters.AddWithValue("personId", personId);
            await command.ExecuteNonQueryAsync();
        }

        return userId;
    }
}