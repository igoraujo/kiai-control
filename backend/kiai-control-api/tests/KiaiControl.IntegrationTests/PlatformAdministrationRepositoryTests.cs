using System.Security.Claims;
using Dapper;
using KiaiControl.Api.Middlewares;
using KiaiControl.Api.Services;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Contracts.Platform;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.Auth;
using KiaiControl.UseCases.Organizations;
using KiaiControl.UseCases.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformAdministrationRepositoryTests(PostgreSqlFixture fixture)
{
    private const string Password = "Tmp!ab";
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private ServiceProvider Provider() => new ServiceCollection().AddKiaiRepositories(fixture.ConnectionString).BuildServiceProvider();

    private async Task<OrganizationRegistrationResponse> OrganizationAsync(string? name = null)
    {
        using var services = Provider();
        return (await new OrganizationUseCase(services.GetRequiredService<IOrganizationRepository>(), _hasher).RegisterAsync(new()
        {
            Name = name ?? $"Plataforma teste {Guid.NewGuid():N}", OwnerFullName = "Responsável 'teste%",
            OwnerEmail = $"owner-{Guid.NewGuid():N}@example.test", OwnerPassword = Password
        }, default))!;
    }

    private static AuditLog Audit(Guid actor, string action = "platform.account.updated", Guid? target = null) => new()
    {
        ActorUserAccountId = actor, Action = action, EntityName = "platform.accounts", EntityId = target,
        Metadata = "{\"reason\":\"Validação administrativa\"}"
    };

    [Fact]
    public async Task OrganizationRegistration_ShouldRollbackWhenAuditFailsOrEmailConflicts()
    {
        using var services = Provider();
        var useCase = new OrganizationUseCase(services.GetRequiredService<IOrganizationRepository>(), _hasher);
        var existing = await OrganizationAsync();
        var name = $"Rollback {Guid.NewGuid():N}";
        var request = new RegisterOrganizationRequest { Name = name, OwnerFullName = "Novo owner", OwnerEmail = $"new-{Guid.NewGuid():N}@example.test", OwnerPassword = Password };
        await Assert.ThrowsAsync<PostgresException>(() => useCase.RegisterAsync(request, default, Audit(Guid.NewGuid())));
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        Assert.Equal(0, await connection.QuerySingleAsync<int>("select count(1) from organizations where name = @name", new { name }));
        request.OwnerEmail = existing.OwnerEmail;
        Assert.Null(await useCase.RegisterAsync(request, default, Audit(existing.OwnerUserId)));
        Assert.Equal(0, await connection.QuerySingleAsync<int>("select count(1) from organizations where name = @name", new { name }));
    }

    [Fact]
    public async Task Suspension_ShouldDisableAuthenticationRevokeTokensAndPreserveOtherAcademies()
    {
        var first = await OrganizationAsync();
        var other = await OrganizationAsync();
        using var services = Provider();
        var platform = services.GetRequiredService<IPlatformAdministrationRepository>();
        var auth = services.GetRequiredService<IAuthRepository>();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.ExecuteAsync("""
            insert into refresh_tokens(id,user_account_id,token_hash,expires_at)
            values(gen_random_uuid(),@user,@hash,now()+interval '1 day');
            insert into password_reset_tokens(id,user_account_id,token_hash,expires_at)
            values(gen_random_uuid(),@user,@hash,now()+interval '1 day');
            """, new { user = first.OwnerUserId, hash = Guid.NewGuid().ToString("N") });
        var before = (await auth.GetByIdAsync(first.OwnerUserId))!;
        Assert.True(await platform.SetOrganizationStatusAsync(first.Organization.Id, "suspended", Audit(other.OwnerUserId, "platform.organization.status_changed"), default));
        Assert.False((await auth.GetByEmailAsync(first.OwnerEmail))!.IsActive);
        Assert.False((await auth.GetByIdAsync(first.OwnerUserId))!.IsActive);
        Assert.True((await auth.GetByIdAsync(other.OwnerUserId))!.IsActive);
        Assert.Equal(0, await connection.QuerySingleAsync<int>("select count(1) from refresh_tokens where user_account_id=@user and revoked_at is null", new { user = first.OwnerUserId }));
        Assert.Equal(0, await connection.QuerySingleAsync<int>("select count(1) from password_reset_tokens where user_account_id=@user and revoked_at is null", new { user = first.OwnerUserId }));
        Assert.True(await platform.SetOrganizationStatusAsync(first.Organization.Id, "active", Audit(other.OwnerUserId), default));
        var restored = (await auth.GetByIdAsync(first.OwnerUserId))!;
        Assert.True(restored.IsActive);
        Assert.Equal(before.SessionVersion + 2, restored.SessionVersion);
    }

    [Fact]
    public async Task LastActiveOwner_ShouldBeProtectedIncludingConcurrentBlocks()
    {
        var organization = await OrganizationAsync();
        var actor = await OrganizationAsync();
        using var services = Provider();
        var platform = services.GetRequiredService<IPlatformAdministrationRepository>();
        await Assert.ThrowsAsync<ConflictException>(() => platform.UpdateAccountAsync(organization.Organization.Id, organization.OwnerUserId,
            organization.OwnerEmail, ["admin"], true, Audit(actor.OwnerUserId), default));
        Assert.True(await platform.CreateAccountAsync(organization.Organization.Id, "Outro owner", $"second-{Guid.NewGuid():N}@example.test",
            _hasher.Hash(Password), "owner", Audit(actor.OwnerUserId), default));
        var accounts = (await platform.ListAccountsAsync(organization.Organization.Id, 1, 25, null, default)).Items;
        var blocks = await Task.WhenAll(accounts.Select(async account =>
        {
            try { return await platform.UpdateAccountAsync(organization.Organization.Id, account.UserId, account.Email, ["owner"], false, Audit(actor.OwnerUserId), default); }
            catch (ConflictException) { return false; }
        }));
        Assert.Single(blocks, x => x);
        Assert.Single((await platform.ListAccountsAsync(organization.Organization.Id, 1, 25, null, default)).Items, x => x.IsActive && x.Roles.Contains("owner"));
    }

    [Fact]
    public async Task AccountChanges_ShouldBeScopedAndNeverGrantPlatformOrUnlinkedStudentProfiles()
    {
        var organization = await OrganizationAsync();
        var other = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        var useCase = new PlatformAdministrationUseCase(repository, _hasher);
        Assert.False(await repository.ResetPasswordAsync(other.Organization.Id, organization.OwnerUserId, "hash", Audit(other.OwnerUserId), default));
        Assert.False(await repository.ResetPasswordAsync(organization.Organization.Id, organization.OwnerUserId, "hash", Audit(organization.OwnerUserId), default));
        Assert.False(await repository.SetOrganizationStatusAsync(PlatformIdentity.OrganizationId, "suspended", Audit(other.OwnerUserId), default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.UpdateAccountAsync(organization.Organization.Id, organization.OwnerUserId,
            new() { Email = organization.OwnerEmail, IsActive = true, Roles = ["platform_owner"], Reason = "Tentativa de elevação" }, Audit(other.OwnerUserId), default));
        await Assert.ThrowsAsync<ConflictException>(() => repository.UpdateAccountAsync(organization.Organization.Id, organization.OwnerUserId,
            organization.OwnerEmail, ["owner", "client"], true, Audit(other.OwnerUserId), default));
    }

    [Fact]
    public async Task PasswordReset_ShouldHashPasswordForceChangeAndExcludeSecretsFromAudit()
    {
        var organization = await OrganizationAsync();
        var actor = await OrganizationAsync();
        using var services = Provider();
        var useCase = new PlatformAdministrationUseCase(services.GetRequiredService<IPlatformAdministrationRepository>(), _hasher);
        Assert.True(await useCase.ResetPasswordAsync(organization.Organization.Id, organization.OwnerUserId,
            new() { TemporaryPassword = "New!ab", Reason = "Recuperação de acesso" }, Audit(actor.OwnerUserId, "platform.account.password_reset"), default));
        var account = (await services.GetRequiredService<IAuthRepository>().GetByIdAsync(organization.OwnerUserId))!;
        Assert.True(_hasher.Verify("New!ab", account.PasswordHash));
        Assert.False(_hasher.Verify(Password, account.PasswordHash));
        Assert.True(account.MustChangePassword);
        Assert.Equal(1, account.SessionVersion);
        var log = Assert.Single(await services.GetRequiredService<IAuditLogRepository>().ListAsync(organization.Organization.Id, null, null, 1, 25));
        Assert.DoesNotContain("New!ab", log.Metadata);
        Assert.DoesNotContain(account.PasswordHash, log.Metadata);
    }

    [Fact]
    public async Task AuditFailure_ShouldRollbackStatusAndSessionRevocation()
    {
        var organization = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        await Assert.ThrowsAsync<PostgresException>(() => repository.SetOrganizationStatusAsync(organization.Organization.Id, "suspended", Audit(Guid.NewGuid()), default));
        var account = (await services.GetRequiredService<IAuthRepository>().GetByIdAsync(organization.OwnerUserId))!;
        Assert.True(account.IsActive);
        Assert.Equal(0, account.SessionVersion);
        Assert.Equal("active", (await services.GetRequiredService<IOrganizationRepository>().GetByIdAsync(organization.Organization.Id))!.Status);
    }

    [Fact]
    public async Task OrganizationEdit_ShouldPreserveSuspensionAndAuditAccurateBeforeAndAfterValues()
    {
        var organization = await OrganizationAsync();
        var actor = await OrganizationAsync();
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        Assert.True(await repository.SetOrganizationStatusAsync(organization.Organization.Id, "suspended", Audit(actor.OwnerUserId), default));
        var update = new Organization { Id = organization.Organization.Id, Name = "Nome atualizado", Phone = "11999990000" };
        Assert.True(await repository.UpdateOrganizationAsync(update, Audit(actor.OwnerUserId, "platform.organization.updated"), default));
        var persisted = (await services.GetRequiredService<IOrganizationRepository>().GetByIdAsync(organization.Organization.Id))!;
        Assert.Equal("suspended", persisted.Status);
        Assert.Equal("Nome atualizado", persisted.Name);
        var logs = await repository.ListAuditAsync(new(organization.Organization.Id, null, "platform.organization.updated", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 25), default);
        using var metadata = System.Text.Json.JsonDocument.Parse(Assert.Single(logs.Items).Metadata!);
        Assert.Equal("suspended", metadata.RootElement.GetProperty("after").GetProperty("Status").GetString());
        Assert.Equal(organization.Organization.Name, metadata.RootElement.GetProperty("before").GetProperty("Name").GetString());
        Assert.Equal(persisted.CreatedAt, metadata.RootElement.GetProperty("after").GetProperty("CreatedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task IncidentCorrelation_ShouldMatchResponseHeaderAndLoggingScope()
    {
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        var context = new DefaultHttpContext();
        var correlation = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Correlation-Id"] = correlation;
        var incident = new PlatformIncidentMiddleware(http => { http.Response.StatusCode = 500; return Task.CompletedTask; });
        var recorder = new PlatformIncidentRecorder(repository, NullLogger<PlatformIncidentRecorder>.Instance);
        var middleware = new CorrelationIdMiddleware(http => incident.InvokeAsync(http, recorder), NullLogger<CorrelationIdMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(correlation, context.TraceIdentifier);
        Assert.Equal(correlation, context.Response.Headers["X-Correlation-Id"]);
        var events = await repository.ListIncidentsAsync(new(null, null, "request_failure", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 200), default);
        Assert.Single(events.Items, x => x.CorrelationId == correlation);
    }

    [Fact]
    public async Task GlobalAudit_ShouldFilterAcrossOrganizationsByActorActionAndPeriodWithStablePages()
    {
        var first = await OrganizationAsync();
        var second = await OrganizationAsync();
        using var services = Provider();
        var auditRepository = services.GetRequiredService<IAuditLogRepository>();
        var actor = (await services.GetRequiredService<IAuthRepository>().GetByIdAsync(first.OwnerUserId))!;
        var now = DateTimeOffset.UtcNow;
        var action = $"test.{Guid.NewGuid():N}";
        foreach (var (org, offset) in new[] { (first.Organization.Id, -3), (second.Organization.Id, -2), (second.Organization.Id, -1) })
        {
            var log = Audit(first.OwnerUserId, action);
            log.OrganizationId = org;
            log.ActorPersonId = actor.PersonId;
            log.CreatedAt = now.AddMinutes(offset);
            await auditRepository.RecordAsync(log);
        }
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        var filter = new PlatformLogFilter(null, first.OwnerUserId, action, now.AddHours(-1), now, 1, 2, "'teste%");
        var page1 = await repository.ListAuditAsync(filter, default);
        var page2 = await repository.ListAuditAsync(filter with { Page = 2 }, default);
        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.Empty(page1.Items.Select(x => x.Id).Intersect(page2.Items.Select(x => x.Id)));
        Assert.All(page1.Items, x => Assert.Equal(first.OwnerEmail, x.ActorEmail));
        Assert.Equal(2, (await repository.ListAuditAsync(filter with { OrganizationId = second.Organization.Id }, default)).Total);
        Assert.Empty((await repository.ListAuditAsync(filter with { ActorSearch = "' OR 1=1 --" }, default)).Items);
        Assert.Empty((await repository.ListAuditAsync(filter with { From = now.AddSeconds(1), To = now.AddHours(1) }, default)).Items);
    }

    [Fact]
    public async Task OrganizationSearch_ShouldTreatWildcardsAndQuotesAsLiteralData()
    {
        var name = $"Academia ' OR 1=1 -- %_ {Guid.NewGuid():N}";
        var organization = await OrganizationAsync(name);
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        Assert.Equal(organization.Organization.Id, Assert.Single((await repository.ListOrganizationsAsync(1, 25, name, "active", default)).Items).Id);
        Assert.Empty((await repository.ListOrganizationsAsync(1, 25, name, "suspended", default)).Items);
        Assert.Empty((await repository.ListOrganizationsAsync(1, 25, Guid.NewGuid().ToString(), null, default)).Items);
        Assert.DoesNotContain((await repository.ListOrganizationsAsync(1, 200, null, null, default)).Items, x => x.Id == PlatformIdentity.OrganizationId);
    }

    [Theory]
    [InlineData(500, "request_failure")]
    [InlineData(401, "authentication_failure")]
    public async Task IncidentMiddleware_ShouldPersistSafeRouteWithoutBodyQueryOrCredentials(int status, string category)
    {
        using var services = Provider();
        var repository = services.GetRequiredService<IPlatformAdministrationRepository>();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = status == 401 ? "/api/v1/auth/login" : "/api/v1/test/secret-value";
        context.Request.QueryString = new QueryString("?password=secret-value");
        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("api/v1/test/{id}"), 0, EndpointMetadataCollection.Empty, "test"));
        var middleware = new PlatformIncidentMiddleware(http => { http.Response.StatusCode = status; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, new PlatformIncidentRecorder(repository, NullLogger<PlatformIncidentRecorder>.Instance));
        var result = await repository.ListIncidentsAsync(new(null, null, category, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), 1, 200), default);
        var incident = Assert.Single(result.Items, x => x.CorrelationId == context.TraceIdentifier);
        Assert.Equal("api/v1/test/{id}", incident.Route);
        Assert.DoesNotContain("secret-value", incident.Route);
    }

    [Theory]
    [InlineData(0, 25, 1)]
    [InlineData(1, 201, 1)]
    [InlineData(1, 25, 367)]
    [InlineData(1, 25, -1)]
    public void GlobalLogFilter_ShouldRejectUnboundedOrInvalidRequests(int page, int size, int days)
    {
        var from = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => PlatformAdministrationUseCase.ValidateFilter(new(null, null, null, from, from.AddDays(days), page, size)));
    }
}
