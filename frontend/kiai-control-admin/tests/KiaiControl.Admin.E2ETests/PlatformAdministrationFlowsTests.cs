using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Contracts.Platform;
using Microsoft.Playwright;

namespace KiaiControl.Admin.E2ETests;

public sealed class PlatformAdministrationFlowsTests
{
    private const string Temporary = "Tmp!ab";
    private const string Personal = "New!ab";

    private static async Task<SessionTokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<SessionTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return session;
    }

    private static async Task ConfirmLifecycleAsync(IPage page, string action, string name, string reason)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = action, Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Confirme o nome da academia", new() { Exact = true }).FillAsync(name);
        await dialog.GetByLabel("Motivo e fundamento", new() { Exact = true }).FillAsync(reason);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Confirmar operação", Exact = true }).ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task ReturningContract_ShouldRestoreAndReactivateExistingAcademyAndAllowNewLogin()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var platform = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        using var owner = new HttpClient { BaseAddress = platform.BaseAddress };
        await LoginAsync(platform, env.PlatformEmail, env.PlatformPassword);
        var originalSession = await LoginAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var id = originalSession.User.OrganizationId;
        var academy = (await platform.GetFromJsonAsync<OrganizationResponse>($"platform/organizations/{id}"))!;
        using var created = await owner.PostAsJsonAsync("clients", new { fullName = "Aluno antes do cancelamento" });
        created.EnsureSuccessStatusCode();
        var student = (await created.Content.ReadFromJsonAsync<ClientResponse>())!;
        var accountsBefore = (await platform.GetFromJsonAsync<PagedResponse<PlatformAccountResponse>>($"platform/organizations/{id}/accounts"))!;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(env.AdminUrl + "/login");
        await page.GetByLabel("E-mail", new() { Exact = true }).FillAsync(env.PlatformEmail);
        await page.GetByLabel("Senha", new() { Exact = true }).FillAsync(env.PlatformPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/platform/organizations");
        await page.GotoAsync($"{env.AdminUrl}/platform/organizations/{id}");
        await ConfirmLifecycleAsync(page, "Arquivar academia", academy.Name, "Cancelamento do contrato pelo cliente");
        await page.GetByRole(AriaRole.Button, new() { Name = "Restaurar e reativar", Exact = true }).WaitForAsync();
        using (var blocked = await owner.PostAsJsonAsync("auth/login", new { email = env.OwnerEmail, password = env.OwnerPassword })) Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        await ConfirmLifecycleAsync(page, "Restaurar e reativar", academy.Name, "Cliente retomou o contrato com a academia");
        await page.GetByRole(AriaRole.Button, new() { Name = "Arquivar academia", Exact = true }).WaitForAsync();
        var restored = (await platform.GetFromJsonAsync<OrganizationResponse>($"platform/organizations/{id}"))!;
        Assert.Equal(id, restored.Id);
        Assert.Equal("active", restored.Status);
        Assert.Null(restored.DeletedAt);
        using (var staleJwt = await owner.GetAsync("auth/me")) Assert.Equal(HttpStatusCode.Unauthorized, staleJwt.StatusCode);
        using (var staleRefresh = await owner.PostAsJsonAsync("auth/refresh", new { refreshToken = originalSession.RefreshToken })) Assert.Equal(HttpStatusCode.Unauthorized, staleRefresh.StatusCode);
        var resumed = await LoginAsync(owner, env.OwnerEmail, env.OwnerPassword);
        Assert.Equal(originalSession.User.UserId, resumed.User.UserId);
        Assert.Equal(originalSession.User.PersonId, resumed.User.PersonId);
        Assert.Equal(id, resumed.User.OrganizationId);
        var students = (await owner.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients"))!;
        Assert.Equal(student.Id, Assert.Single(students.Items).Id);
        var accountsAfter = (await platform.GetFromJsonAsync<PagedResponse<PlatformAccountResponse>>($"platform/organizations/{id}/accounts"))!;
        Assert.Equal(accountsBefore.Total, accountsAfter.Total);
        Assert.Equal(accountsBefore.Items.Select(x => x.UserId), accountsAfter.Items.Select(x => x.UserId));
        var organizations = (await platform.GetFromJsonAsync<PagedResponse<OrganizationResponse>>("platform/organizations?archive=all"))!;
        Assert.Equal(id, Assert.Single(organizations.Items).Id);
        var audit = (await platform.GetFromJsonAsync<PagedResponse<PlatformAuditResponse>>($"platform/audit-logs?organizationId={id}&action=platform.organization.reactivated"))!;
        Assert.Single(audit.Items);
    }

    [Fact]
    public async Task TenantRoles_ShouldBeDeniedEveryPlatformContextAndMutation()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var owner = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        var ownerSession = await LoginAsync(owner, env.OwnerEmail, env.OwnerPassword);
        foreach (var role in new[] { "admin", "reception", "client", "teacher" })
        {
            var email = $"{role}-{Guid.NewGuid():N}@example.test";
            var resource = role switch { "client" => "clients", "teacher" => "teachers", _ => "people" };
            using var created = await owner.PostAsJsonAsync(resource, new { fullName = $"Teste papel {role}" });
            created.EnsureSuccessStatusCode();
            var person = await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var personId = person.GetProperty("personId").GetGuid();
            using var access = await owner.PostAsJsonAsync($"access/people/{personId}/account", new { email, temporaryPassword = Temporary, roleCode = role });
            access.EnsureSuccessStatusCode();
            using var actor = new HttpClient { BaseAddress = owner.BaseAddress };
            await LoginAsync(actor, email, Temporary);
            using var change = await actor.PostAsJsonAsync("auth/change-password", new { currentPassword = Temporary, newPassword = Personal });
            change.EnsureSuccessStatusCode();
            await LoginAsync(actor, email, Personal);
            actor.DefaultRequestHeaders.Add("X-Organization-Id", "da6ad84c-d663-47a8-b3ae-f3abcf7f1020");
            foreach (var path in new[] { "platform/organizations", $"platform/organizations/{ownerSession.User.OrganizationId}/accounts", "platform/audit-logs", "platform/incidents", "platform/overview", "platform/metrics", "platform/health", "platform/retention-policy" })
            {
                using var denied = await actor.GetAsync(path);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            }
            using var deniedMutation = await actor.PutAsJsonAsync($"platform/organizations/{ownerSession.User.OrganizationId}/accounts/{ownerSession.User.UserId}/password", new { temporaryPassword = "Hack!ab", reason = "Tentativa não autorizada" });
            Assert.Equal(HttpStatusCode.Forbidden, deniedMutation.StatusCode);
        }
    }

    [Fact]
    public async Task Platform_ShouldManageAcademiesAccountsAuditAndIncidentsThroughBrowserAndApi()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var platform = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        await LoginAsync(platform, env.PlatformEmail, env.PlatformPassword);
        using var tenant = new HttpClient { BaseAddress = platform.BaseAddress };
        var ownerSession = await LoginAsync(tenant, env.OwnerEmail, env.OwnerPassword);
        foreach (var resource in new[] { "platform/organizations", "platform/audit-logs", "platform/incidents", "platform/overview" })
        {
            using var denied = await tenant.GetAsync(resource);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        tenant.DefaultRequestHeaders.Add("X-Organization-Id", "da6ad84c-d663-47a8-b3ae-f3abcf7f1020");
        using (var denied = await tenant.PutAsJsonAsync($"platform/organizations/{ownerSession.User.OrganizationId}/status", new { status = "suspended", reason = "Tentativa pelo owner" }))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        platform.DefaultRequestHeaders.Add("X-Organization-Id", ownerSession.User.OrganizationId.ToString());
        using (var denied = await platform.GetAsync("catalog/clients")) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(env.AdminUrl + "/login");
        await page.GetByLabel("E-mail", new() { Exact = true }).FillAsync(env.PlatformEmail);
        await page.GetByLabel("Senha", new() { Exact = true }).FillAsync(env.PlatformPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/platform/organizations");
        await page.GetByRole(AriaRole.Link, new() { Name = "Auditoria global", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Nova academia", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        var academy = $"Academia plataforma {Guid.NewGuid():N}";
        var email = $"platform-test-{Guid.NewGuid():N}@example.test";
        await dialog.GetByLabel("Nome da academia", new() { Exact = true }).FillAsync(academy);
        await dialog.GetByLabel("Nome do responsável", new() { Exact = true }).FillAsync("Owner inicial");
        await dialog.GetByLabel("E-mail de acesso", new() { Exact = true }).FillAsync(email);
        await dialog.GetByLabel("Senha temporária", new() { Exact = true }).FillAsync(Temporary);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cadastrar academia", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = academy, Exact = true }).WaitForAsync();
        var organizationId = Guid.Parse(new Uri(page.Url).AbsolutePath.Split('/').Last());
        await page.GetByRole(AriaRole.Button, new() { Name = "Editar dados", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Telefone", new() { Exact = true }).FillAsync("11999990000");
        await dialog.GetByLabel("Motivo da alteração", new() { Exact = true }).FillAsync("Atualização de contato de suporte");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Salvar alteração", Exact = true }).ClickAsync();
        await page.GetByText("11999990000", new() { Exact = false }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar responsável", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        var secondEmail = $"second-{Guid.NewGuid():N}@example.test";
        await dialog.GetByLabel("Nome do responsável", new() { Exact = true }).FillAsync("Owner adicional");
        await dialog.GetByLabel("E-mail de acesso", new() { Exact = true }).FillAsync(secondEmail);
        await dialog.GetByLabel("Senha temporária", new() { Exact = true }).FillAsync(Temporary);
        await dialog.GetByLabel("Motivo da alteração", new() { Exact = true }).FillAsync("Adicionar responsável de suporte");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Criar responsável", Exact = true }).ClickAsync();
        await page.GetByText(secondEmail, new() { Exact = true }).WaitForAsync();

        using var newOwner = new HttpClient { BaseAddress = platform.BaseAddress };
        Assert.True((await LoginAsync(newOwner, email, Temporary)).User.MustChangePassword);
        using (var change = await newOwner.PostAsJsonAsync("auth/change-password", new { currentPassword = Temporary, newPassword = Personal })) change.EnsureSuccessStatusCode();
        var activeSession = await LoginAsync(newOwner, email, Personal);
        var initialRow = page.GetByRole(AriaRole.Row).Filter(new() { HasText = email });
        await initialRow.GetByRole(AriaRole.Button, new() { Name = "Gerenciar acesso", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Senha temporária", new() { Exact = true }).FillAsync("Reset!ab");
        await dialog.GetByLabel("Motivo da alteração", new() { Exact = true }).FillAsync("Redefinição solicitada no suporte");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Redefinir senha", Exact = true }).ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        using (var oldSession = await newOwner.GetAsync("auth/me")) Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        using (var oldRefresh = await newOwner.PostAsJsonAsync("auth/refresh", new { refreshToken = activeSession.RefreshToken })) Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
        var resetSession = await LoginAsync(newOwner, email, "Reset!ab");
        Assert.True(resetSession.User.MustChangePassword);
        using (var change = await newOwner.PostAsJsonAsync("auth/change-password", new { currentPassword = "Reset!ab", newPassword = Personal })) change.EnsureSuccessStatusCode();
        await LoginAsync(newOwner, email, Personal);

        await page.GetByRole(AriaRole.Button, new() { Name = "Alterar status", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByRole(AriaRole.Combobox, new() { Name = "Novo status", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Suspensa", Exact = true }).ClickAsync();
        await dialog.GetByLabel("Motivo da alteração", new() { Exact = true }).FillAsync("Suspensão para manutenção programada");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Salvar alteração", Exact = true }).ClickAsync();
        await page.GetByText("O acesso dos usuários desta academia está impedido.", new() { Exact = false }).WaitForAsync();
        using (var denied = await newOwner.GetAsync("auth/me")) Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using (var denied = await newOwner.PostAsJsonAsync("auth/login", new { email, password = Personal })) Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using (var reactivate = await platform.PutAsJsonAsync($"platform/organizations/{organizationId}/status", new { status = "active", reason = "Manutenção concluída com sucesso" })) reactivate.EnsureSuccessStatusCode();
        await LoginAsync(newOwner, email, Personal);

        var logs = (await platform.GetFromJsonAsync<PagedResponse<PlatformAuditResponse>>($"platform/audit-logs?organizationId={organizationId}&action=platform.account.password_reset"))!;
        var resetLog = Assert.Single(logs.Items);
        Assert.Null(resetLog.ActorEmail);
        Assert.Null(resetLog.Metadata);
        using (var detailsResponse = await platform.PostAsJsonAsync($"platform/audit-logs/{resetLog.Id}/details", new { organizationId }))
        {
            detailsResponse.EnsureSuccessStatusCode();
            var details = (await detailsResponse.Content.ReadFromJsonAsync<PlatformAuditResponse>())!;
            Assert.Equal(env.PlatformEmail, details.ActorEmail);
            Assert.DoesNotContain("Reset!ab", details.Metadata);
        }
        await page.GotoAsync(env.AdminUrl + "/platform/audit");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Auditoria global", Exact = true }).WaitForAsync();
        await page.GetByLabel("Ação", new() { Exact = true }).FillAsync("platform.account.password_reset");
        await page.GetByRole(AriaRole.Button, new() { Name = "Aplicar filtros", Exact = true }).ClickAsync();
        var resetRow = page.GetByRole(AriaRole.Row).Filter(new() { HasText = "platform.account.password_reset" });
        await resetRow.GetByRole(AriaRole.Button, new() { Name = "Ver detalhes", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByText("Redefinição solicitada no suporte", new() { Exact = false }).WaitForAsync();
        Assert.Equal(0, await dialog.GetByRole(AriaRole.Textbox).CountAsync());
        Assert.Equal(0, await dialog.GetByRole(AriaRole.Button, new() { Name = "Carregar detalhes", Exact = true }).CountAsync());
        Assert.DoesNotContain("Reset!ab", await dialog.InnerTextAsync());
        var detailReads = (await platform.GetFromJsonAsync<PagedResponse<PlatformAuditResponse>>($"platform/audit-logs?organizationId={organizationId}&action=platform.audit.details_viewed"))!;
        Assert.Equal(2, detailReads.Total);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Fechar", Exact = true }).ClickAsync();
        await page.GotoAsync(env.AdminUrl + "/platform/operations");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Operação da plataforma", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Cell, new() { Name = "Falha de login · HTTP 401", Exact = false }).First.WaitForAsync();
        foreach (var (path, heading) in new[]
        {
            ("/platform/organizations", "Academias"),
            ($"/platform/organizations/{organizationId}", academy),
            ("/platform/audit", "Auditoria global"),
            ("/platform/operations", "Operação da plataforma")
        })
        {
            await page.SetViewportSizeAsync(390, 844);
            await page.GotoAsync(env.AdminUrl + path);
            await page.GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true }).WaitForAsync();
            await page.WaitForTimeoutAsync(500);
            var screenshotDirectory = Path.Combine(Path.GetTempPath(), "kiai-platform-preview");
            Directory.CreateDirectory(screenshotDirectory);
            var fileName = path.EndsWith(organizationId.ToString()) ? "academy" : path.Split('/').Last();
            await page.ScreenshotAsync(new() { Path = Path.Combine(screenshotDirectory, fileName + "-mobile.png"), FullPage = true });
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"), path);
        }
    }

    [Fact]
    public async Task Platform_ShouldArchiveRestoreAndKeepAcademyAuditScopeWithPrivateDefaults()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var platform = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        await LoginAsync(platform, env.PlatformEmail, env.PlatformPassword);
        using var owner = new HttpClient { BaseAddress = platform.BaseAddress };
        var ownerSession = await LoginAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var id = ownerSession.User.OrganizationId;
        var academy = (await platform.GetFromJsonAsync<OrganizationResponse>($"platform/organizations/{id}"))!;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(env.AdminUrl + "/login");
        await page.GetByLabel("E-mail", new() { Exact = true }).FillAsync(env.PlatformEmail);
        await page.GetByLabel("Senha", new() { Exact = true }).FillAsync(env.PlatformPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/platform/organizations");
        await page.GotoAsync($"{env.AdminUrl}/platform/organizations/{id}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Arquivar academia", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        var name = academy.Name;
        await dialog.GetByLabel("Confirme o nome da academia", new() { Exact = true }).FillAsync(name);
        await dialog.GetByLabel("Motivo e fundamento", new() { Exact = true }).FillAsync("Encerramento contratual documentado para teste");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Confirmar operação", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Restaurar academia", Exact = true }).WaitForAsync();
        using (var denied = await owner.GetAsync("auth/me")) Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await page.GetByRole(AriaRole.Link, new() { Name = "Auditoria da academia", Exact = true }).ClickAsync();
        await page.WaitForURLAsync($"{env.AdminUrl}/platform/organizations/{id}/audit");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Auditoria da academia", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "platform.organization.archived" }).WaitForAsync();
        Assert.Equal(0, await page.GetByRole(AriaRole.Combobox, new() { Name = "Academia", Exact = true }).CountAsync());
        Assert.Equal(0, await page.GetByRole(AriaRole.Cell, new() { Name = env.PlatformEmail, Exact = true }).CountAsync());
        var listed = (await platform.GetFromJsonAsync<PagedResponse<OrganizationResponse>>("platform/organizations?archive=archived"))!;
        Assert.Equal(id, Assert.Single(listed.Items).Id);
        using var wrong = await platform.PostAsJsonAsync($"platform/organizations/{id}/restore", new { confirmName = "Wrong", reason = "Tentativa de restauração" });
        Assert.Equal(HttpStatusCode.Conflict, wrong.StatusCode);
        await page.GotoAsync($"{env.AdminUrl}/platform/organizations/{id}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Restaurar academia", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Confirme o nome da academia", new() { Exact = true }).FillAsync(name);
        await dialog.GetByLabel("Motivo e fundamento", new() { Exact = true }).FillAsync("Restauração administrativa documentada");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Confirmar operação", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Arquivar academia", Exact = true }).WaitForAsync();
        var restored = (await platform.GetFromJsonAsync<OrganizationResponse>($"platform/organizations/{id}"))!;
        Assert.Null(restored.DeletedAt);
        Assert.Equal("inactive", restored.Status);
        using var ready = await new HttpClient().GetAsync(env.ApiUrl + "/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var health = (await platform.GetFromJsonAsync<PlatformHealthResponse>("platform/health"))!;
        Assert.Equal("Healthy", health.Status);
        Assert.Contains(health.Dependencies, x => x.Name == "PostgreSQL");
        PlatformMetricsResponse? metrics = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            metrics = await platform.GetFromJsonAsync<PlatformMetricsResponse>($"platform/metrics?organizationId={id}");
            if (metrics!.Requests > 0) break;
            await Task.Delay(100);
        }
        Assert.True(metrics!.Requests > 0);
        Assert.Equal(id, Assert.Single(metrics.Academies).Id);
        await page.GotoAsync($"{env.AdminUrl}/platform/operations?organizationId={id}");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Privacidade e retenção", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Atualizar operação", Exact = true }).ClickAsync();
        await page.SetViewportSizeAsync(390, 844);
        await page.WaitForFunctionAsync("document.documentElement.scrollWidth <= window.innerWidth + 1", null, new() { Timeout = 5000 });
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth + 1"));
    }

    [Fact]
    public async Task Readiness_ShouldFailWhenDatabaseStopsWhileLivenessRemainsAvailable()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(env.ApiUrl), Timeout = TimeSpan.FromSeconds(15) };
        using (var ready = await client.GetAsync("/health/ready")) Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        await env.StopDatabaseAsync();
        using (var live = await client.GetAsync("/health/live")) Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        using (var ready = await client.GetAsync("/health/ready"))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
            Assert.DoesNotContain("Password", await ready.Content.ReadAsStringAsync());
        }
    }
}
