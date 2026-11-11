using Dapper;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Contracts.Platform;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.Auth;
using KiaiControl.UseCases.Organizations;
using KiaiControl.UseCases.Platform;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformLifecycleObservabilityTests(PostgreSqlFixture fixture)
{
    private ServiceProvider Provider() => new ServiceCollection().AddKiaiRepositories(fixture.ConnectionString).BuildServiceProvider();
    private async Task<OrganizationRegistrationResponse> OrganizationAsync()
    {
        using var services = Provider();
        return (await new OrganizationUseCase(services.GetRequiredService<IOrganizationRepository>(), new Pbkdf2PasswordHasher()).RegisterAsync(new()
        { Name = $"Academia ciclo {Guid.NewGuid():N}", OwnerFullName = "Responsável teste", OwnerEmail = $"owner-{Guid.NewGuid():N}@example.test", OwnerPassword = "Tmp!ab" }, default))!;
    }
    private static AuditLog Audit(Guid actor, string action = "platform.organization.archived") => new()
    { ActorUserAccountId = actor, Action = action, EntityName = "platform.organizations", Metadata = "{\"reason\":\"Operação administrativa de teste\"}" };

    [Fact]
    public async Task ArchiveRestore_ShouldPreserveHistoryRevokeAccessAndRequireExplicitReactivation()
    {
        var academy = await OrganizationAsync();
        var actor = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        Assert.True(await repository.ChangeArchiveAsync(academy.Organization.Id, true, academy.Organization.Name, Audit(actor.OwnerUserId), default));
        Assert.NotNull((await repository.GetOrganizationAsync(academy.Organization.Id, default))!.DeletedAt);
        Assert.Null(await services.GetRequiredService<IOrganizationRepository>().GetByIdAsync(academy.Organization.Id));
        Assert.False((await services.GetRequiredService<IAuthRepository>().GetByIdAsync(academy.OwnerUserId))!.IsActive);
        Assert.Single((await repository.ListOrganizationsAsync(1, 25, academy.Organization.Name, null, default, "archived")).Items);
        Assert.Empty((await repository.ListOrganizationsAsync(1, 25, academy.Organization.Name, null, default)).Items);
        Assert.Single((await repository.ListAuditAsync(new(academy.Organization.Id, null, null, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 25), default)).Items);
        Assert.False(await repository.ResetPasswordAsync(academy.Organization.Id, academy.OwnerUserId, "hash", Audit(actor.OwnerUserId), default));
        Assert.True(await repository.ChangeArchiveAsync(academy.Organization.Id, false, academy.Organization.Name, Audit(actor.OwnerUserId, "platform.organization.restored"), default));
        var restored = (await repository.GetOrganizationAsync(academy.Organization.Id, default))!;
        Assert.Null(restored.DeletedAt);
        Assert.Equal("inactive", restored.Status);
        Assert.False((await services.GetRequiredService<IAuthRepository>().GetByIdAsync(academy.OwnerUserId))!.IsActive);
        Assert.True(await repository.SetOrganizationStatusAsync(academy.Organization.Id, "active", Audit(actor.OwnerUserId), default));
        Assert.True((await services.GetRequiredService<IAuthRepository>().GetByIdAsync(academy.OwnerUserId))!.IsActive);
        Assert.True((await services.GetRequiredService<IAuthRepository>().GetByIdAsync(actor.OwnerUserId))!.IsActive);
    }

    [Fact]
    public async Task Archive_ShouldRequireConfirmationAndRollbackWhenAuditFails()
    {
        var academy = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        await Assert.ThrowsAsync<ConflictException>(() => repository.ChangeArchiveAsync(academy.Organization.Id, true, "Wrong name", Audit(academy.OwnerUserId), default));
        await Assert.ThrowsAsync<PostgresException>(() => repository.ChangeArchiveAsync(academy.Organization.Id, true, academy.Organization.Name, Audit(Guid.NewGuid()), default));
        Assert.Null((await repository.GetOrganizationAsync(academy.Organization.Id, default))!.DeletedAt);
        Assert.Equal(0, (await services.GetRequiredService<IAuthRepository>().GetByIdAsync(academy.OwnerUserId))!.SessionVersion);
        Assert.False(await repository.ChangeArchiveAsync(PlatformIdentity.OrganizationId, true, "Platform", Audit(academy.OwnerUserId), default));
    }

    [Fact]
    public async Task ContractReturn_ShouldReactivateSameAcademyPreserveClientsAndKeepBlockedAccountsBlocked()
    {
        var academy = await OrganizationAsync();
        var actor = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        var auth = services.GetRequiredService<IAuthRepository>();
        var useCase = new PlatformAdministrationUseCase(repository, new Pbkdf2PasswordHasher());
        var originalOwner = (await auth.GetByIdAsync(academy.OwnerUserId))!;
        var personId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var blockedUserId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        const string seed = """
            insert into persons (id, organization_id, full_name)
            values (@PersonId, @OrganizationId, 'Aluno com histórico preservado');
            insert into clients (id, organization_id, person_id)
            values (@ClientId, @OrganizationId, @PersonId);
            insert into user_accounts (id, person_id, email, password_hash, is_active)
            values (@UserId, @PersonId, @Email, 'blocked-test-account', false);
            """;
        await connection.ExecuteAsync(seed, new { PersonId = personId, ClientId = clientId, UserId = blockedUserId, OrganizationId = academy.Organization.Id, Email = $"blocked-{Guid.NewGuid():N}@example.test" });
        Assert.True(await repository.ChangeArchiveAsync(academy.Organization.Id, true, academy.Organization.Name, Audit(actor.OwnerUserId), default));
        await Assert.ThrowsAsync<PostgresException>(() => repository.ChangeArchiveAsync(academy.Organization.Id, false, academy.Organization.Name,
            Audit(Guid.NewGuid(), "platform.organization.reactivated"), default, reactivate: true));
        Assert.NotNull((await repository.GetOrganizationAsync(academy.Organization.Id, default))!.DeletedAt);
        Assert.False((await auth.GetByIdAsync(academy.OwnerUserId))!.IsActive);

        var request = new PlatformArchiveRequest { ConfirmName = academy.Organization.Name, Reason = "Retomada do contrato da academia", Reactivate = true };
        Assert.True(await useCase.ChangeArchiveAsync(academy.Organization.Id, false, request, Audit(actor.OwnerUserId, "platform.organization.reactivated"), default));
        var restored = (await repository.GetOrganizationAsync(academy.Organization.Id, default))!;
        Assert.Equal(academy.Organization.Id, restored.Id);
        Assert.Equal("active", restored.Status);
        Assert.Null(restored.DeletedAt);
        var ownerAfter = (await auth.GetByIdAsync(academy.OwnerUserId))!;
        Assert.True(ownerAfter.IsActive);
        Assert.Equal(originalOwner.PersonId, ownerAfter.PersonId);
        Assert.Equal(originalOwner.PasswordHash, ownerAfter.PasswordHash);
        Assert.Equal(originalOwner.MustChangePassword, ownerAfter.MustChangePassword);
        Assert.False((await auth.GetByIdAsync(blockedUserId))!.IsActive);
        Assert.Equal(clientId, await connection.QuerySingleAsync<Guid>("select id from clients where organization_id=@OrganizationId and person_id=@PersonId", new { OrganizationId = academy.Organization.Id, PersonId = personId }));
        Assert.Equal(academy.Organization.Id, Assert.Single((await repository.ListOrganizationsAsync(1, 25, academy.Organization.Name, null, default, "all")).Items).Id);
        var logs = await repository.ListAuditAsync(new(academy.Organization.Id, null, null, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 25), default);
        Assert.Contains(logs.Items, entry => entry.Action == "platform.organization.reactivated");
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ChangeArchiveAsync(academy.Organization.Id, true, request, Audit(actor.OwnerUserId), default));
    }

    [Fact]
    public async Task AuditList_ShouldHidePersonalDataAndDetailsShouldOpenWithoutPurposeAndMatchAcademy()
    {
        var academy = await OrganizationAsync();
        var other = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IAuditLogRepository>();
        var actor = (await services.GetRequiredService<IAuthRepository>().GetByIdAsync(academy.OwnerUserId))!;
        var log = Audit(academy.OwnerUserId);
        log.OrganizationId = academy.Organization.Id;
        log.ActorPersonId = actor.PersonId;
        log.IpAddress = "192.0.2.25";
        log.Metadata = "{\"password\":\"never-return\",\"after\":{\"Email\":\"private@example.test\",\"IsActive\":false}}";
        await repository.RecordAsync(log);
        var useCase = new PlatformAdministrationUseCase(services.GetRequiredService<IPlatformAdministrationRepository>(), new Pbkdf2PasswordHasher());
        var listed = Assert.Single((await useCase.ListAuditAsync(new(academy.Organization.Id, null, null, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 25), default)).Items);
        Assert.Null(listed.ActorEmail);
        Assert.Null(listed.ActorUserId);
        Assert.Null(listed.IpAddress);
        Assert.Null(listed.Metadata);
        Assert.Null(listed.EntityId);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GetAuditDetailsAsync(Guid.Empty, new() { OrganizationId = academy.Organization.Id }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GetAuditDetailsAsync(log.Id, new(), default));
        Assert.Null(await useCase.GetAuditDetailsAsync(log.Id, new() { OrganizationId = other.Organization.Id }, default));
        var details = (await useCase.GetAuditDetailsAsync(log.Id, new() { OrganizationId = academy.Organization.Id }, default))!;
        Assert.Equal(academy.OwnerEmail, details.ActorEmail);
        Assert.DoesNotContain("never-return", details.Metadata);
        Assert.DoesNotContain("private@example.test", details.Metadata);
    }

    [Fact]
    public async Task Metrics_ShouldAggregateWeightedDurationsAndScopeAcademyAndPeriod()
    {
        var academy = await OrganizationAsync();
        var other = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformObservabilityRepository>();
        var now = DateTimeOffset.UtcNow;
        var bucket = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        await repository.SaveMetricsAsync([
            new() { OrganizationId = academy.Organization.Id, BucketStart = bucket, Route = "api/v1/clients/{id}", Method = "GET", StatusCode = 200, Requests = 9, DurationTotalMs = 900, MaxDurationMs = 150 },
            new() { OrganizationId = academy.Organization.Id, BucketStart = bucket, Route = "api/v1/clients/{id}", Method = "GET", StatusCode = 500, Requests = 1, DurationTotalMs = 300, MaxDurationMs = 300 },
            new() { OrganizationId = other.Organization.Id, BucketStart = bucket, Route = "api/v1/clients/{id}", Method = "GET", StatusCode = 200, Requests = 20, DurationTotalMs = 4000, MaxDurationMs = 400 }
        ], default);
        var filter = new PlatformLogFilter(academy.Organization.Id, null, null, bucket.AddHours(-1), bucket.AddHours(1), 1, 25);
        var result = await repository.DashboardAsync(filter, default);
        Assert.Equal(10, result.Summary.Requests);
        Assert.Equal(1, result.Summary.ServerErrors);
        Assert.Equal(120, result.Summary.AverageDurationMs);
        Assert.Equal(300, result.Summary.MaxDurationMs);
        Assert.Equal(academy.Organization.Id, Assert.Single(result.Academies.Items).Id);
        Assert.Equal(10, Assert.Single(result.Routes).Requests);
        Assert.Equal(20, (await repository.DashboardAsync(filter with { OrganizationId = other.Organization.Id }, default)).Summary.Requests);
        Assert.Equal(0, (await repository.DashboardAsync(filter with { From = bucket.AddDays(2), To = bucket.AddDays(3) }, default)).Summary.Requests);
    }

    [Fact]
    public async Task Retention_ShouldRemoveExpiredTelemetryRedactAuditAndRespectLegalHoldWithoutDeletingPeople()
    {
        var academy = await OrganizationAsync();
        var held = await OrganizationAsync();
        using var services = Provider();
        var administration = services.GetRequiredService<IPlatformAdministrationRepository>();
        Assert.True(await administration.SetPrivacyHoldAsync(held.Organization.Id, true, Audit(academy.OwnerUserId), default));
        var repository = services.GetRequiredService<IPlatformObservabilityRepository>();
        foreach (var organization in new[] { academy, held })
        {
            await repository.SaveMetricsAsync([new() { OrganizationId = organization.Organization.Id, BucketStart = DateTimeOffset.UtcNow.AddDays(-200), Route = "api/v1/clients", Method = "GET", StatusCode = 200, Requests = 1 }], default);
            await administration.RecordIncidentAsync(new() { OrganizationId = organization.Organization.Id, CreatedAt = DateTimeOffset.UtcNow.AddDays(-200), Method = "GET", Route = "api/v1/clients", StatusCode = 500 }, default);
            await services.GetRequiredService<IAuditLogRepository>().RecordAsync(new()
            {
                OrganizationId = organization.Organization.Id, ActorUserAccountId = organization.OwnerUserId,
                Action = "test.retention", EntityName = "clients", EntityId = Guid.NewGuid(),
                IpAddress = "192.0.2.20", Metadata = "{\"reason\":\"private details\"}", CreatedAt = DateTimeOffset.UtcNow.AddDays(-200)
            });
        }
        var cutoff = DateTimeOffset.UtcNow.AddDays(-180);
        var counts = await repository.ApplyRetentionAsync(cutoff, cutoff, cutoff, default);
        Assert.True(counts.MetricsDeleted >= 1);
        Assert.True(counts.IncidentsDeleted >= 1);
        Assert.True(counts.AuditIdentitiesRedacted >= 1);
        var logs = services.GetRequiredService<IAuditLogRepository>();
        var cleared = Assert.Single(await logs.ListAsync(academy.Organization.Id, null, cutoff, 1, 25));
        Assert.Null(cleared.ActorUserAccountId);
        Assert.Null(cleared.IpAddress);
        Assert.Null(cleared.EntityId);
        Assert.Contains("personalDataRedacted", cleared.Metadata);
        Assert.Equal(held.OwnerUserId, Assert.Single(await logs.ListAsync(held.Organization.Id, null, cutoff, 1, 25)).ActorUserAccountId);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        Assert.Equal(1, await connection.QuerySingleAsync<int>("select count(1) from platform_request_metrics where organization_id=@id", new { id = held.Organization.Id }));
        Assert.Equal(1, await connection.QuerySingleAsync<int>("select count(1) from user_accounts where id=@id", new { id = academy.OwnerUserId }));
        Assert.True(await administration.SetPrivacyHoldAsync(held.Organization.Id, false, Audit(academy.OwnerUserId), default));
        await repository.ApplyRetentionAsync(cutoff, cutoff, cutoff, default);
        Assert.Null(Assert.Single(await logs.ListAsync(held.Organization.Id, null, cutoff, 1, 25)).ActorUserAccountId);
    }
}
