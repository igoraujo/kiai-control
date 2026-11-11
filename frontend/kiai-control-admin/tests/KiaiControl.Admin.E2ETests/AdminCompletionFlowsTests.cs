using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Administration;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Contracts.Billing;
using Microsoft.Playwright;

namespace KiaiControl.Admin.E2ETests;
public sealed class AdminCompletionFlowsTests
{
    private const string Temporary = "Tmp!ab";
    private const string Personal = "Abc!de";
    private static async Task<SessionTokenResponse> SignInAsync(HttpClient client, string email, string password)
    {
        using var r = await client.PostAsJsonAsync("auth/login", new LoginRequest { Email = email, Password = password });
        r.EnsureSuccessStatusCode();
        var session = (await r.Content.ReadFromJsonAsync<SessionTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return session;
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string resource, object body)
    {
        using var r = await client.PostAsJsonAsync(resource, body);
        if (!r.IsSuccessStatusCode)
            throw new InvalidOperationException($"{resource}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task LoginPageAsync(IPage page, string url, string email, string password)
    {
        await page.GotoAsync(url + "/login");
        await page.GetByLabel("E-mail", new() { Exact = true }).FillAsync(email);
        await page.GetByLabel("Senha", new() { Exact = true }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("reception")]
    public async Task ManagementRoles_ShouldSeeGeneralFightAndGymStudentsAndKeepAreaAfterReload(string role)
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var owner = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        await SignInAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var fight = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Jiu-jitsu áreas", Type = "martial_art" });
        var gym = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Musculação áreas", Type = "fitness" });
        var hybrid = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Treinamento híbrido", Type = "hybrid" });
        foreach (var (name, modalities) in new[]
        {
            ("Aluno Fight", new[] { fight.Id }),
            ("Aluno Gym", new[] { gym.Id }),
            ("Aluno Duas", new[] { fight.Id, gym.Id }),
            ("Aluno Híbrido", new[] { hybrid.Id }),
            ("Aluno Sem Modalidade", Array.Empty<Guid>())
        })
        {
            var student = await CreateAsync<ClientResponse>(owner, "clients", new { FullName = name });
            foreach (var modality in modalities)
            {
                using var linked = await owner.PostAsync($"clients/{student.Id}/modalities/{modality}", null);
                linked.EnsureSuccessStatusCode();
            }
        }

        var email = env.OwnerEmail;
        var password = env.OwnerPassword;
        if (role != "owner")
        {
            email = $"{role}-{Guid.NewGuid():N}@example.test";
            var person = await CreateAsync<System.Text.Json.JsonElement>(owner, "people", new { FullName = $"Equipe {role}" });
            await CreateAsync<UserAccountResponse>(owner, $"access/people/{person.GetProperty("personId").GetGuid()}/account", new { Email = email, TemporaryPassword = Temporary, RoleCode = role });
            using var account = new HttpClient { BaseAddress = owner.BaseAddress };
            await SignInAsync(account, email, Temporary);
            using var change = await account.PostAsJsonAsync("auth/change-password", new { CurrentPassword = Temporary, NewPassword = Personal });
            change.EnsureSuccessStatusCode();
            password = Personal;
        }

        using var actor = new HttpClient { BaseAddress = owner.BaseAddress };
        await SignInAsync(actor, email, password);
        Assert.Equal(5, (await actor.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients"))!.Total);
        Assert.Equal(3, (await actor.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients?area=gym"))!.Total);
        var first = await actor.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients?area=fight&pageSize=2");
        var second = await actor.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients?area=fight&pageSize=2&page=2");
        Assert.Equal(3, first!.Total);
        Assert.Equal(3, first.Items.Concat(second!.Items).Select(x => x.Id).Distinct().Count());
        Assert.Empty((await actor.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients?area=gym&search=Fight"))!.Items);
        using (var invalid = await actor.GetAsync("catalog/clients?area=invalid"))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        if (role != "reception")
        {
            var gymOptions = await actor.GetFromJsonAsync<FitnessOptionsResponse>("fitness-goals/options?area=gym");
            Assert.Equal(3, gymOptions!.Clients.Count);
            Assert.DoesNotContain(gymOptions.Clients, x => x.FullName == "Aluno Fight");
            Assert.DoesNotContain(gymOptions.Modalities, x => x.Type == "martial_art");
            var graduation = await actor.GetFromJsonAsync<KiaiControl.Contracts.Graduation.GraduationOptionsResponse>("graduation/options?area=fight");
            Assert.Equal(3, graduation!.Clients.Count);
            Assert.DoesNotContain(graduation.Clients, x => x.FullName == "Aluno Gym");
        }
        else
        {
            using var forbidden = await actor.GetAsync("fitness-goals/options?area=gym");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await LoginPageAsync(page, env.AdminUrl, email, password);
        await page.WaitForURLAsync(env.AdminUrl + (role == "reception" ? "/clients" : "/"));
        await page.GotoAsync(env.AdminUrl + "/fight");
        await page.Locator("a.workspace-card").Filter(new() { HasText = "Jornada dos alunos" }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/clients?area=fight");
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Aluno Fight" }).WaitForAsync();
        Assert.DoesNotContain("Aluno Gym", await page.Locator("body").InnerTextAsync());
        Assert.DoesNotContain("Aluno Sem Modalidade", await page.Locator("body").InnerTextAsync());
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Visão da área" }).GetByRole(AriaRole.Link, new() { Name = "Gym", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Aluno Gym" }).WaitForAsync();
        Assert.DoesNotContain("Aluno Fight", await page.Locator("body").InnerTextAsync());
        await page.ReloadAsync();
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Aluno Gym" }).WaitForAsync();
        Assert.EndsWith("/clients?area=gym", page.Url);
        Assert.Equal("page", await page.GetByRole(AriaRole.Navigation, new() { Name = "Visão da área" }).GetByRole(AriaRole.Link, new() { Name = "Gym", Exact = true }).GetAttributeAsync("aria-current"));
        Assert.Equal(2, await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Aluno Duas" }).Locator(".client-area-tag").CountAsync());
        if (role == "owner")
        {
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "kiai-admin-preview"));
            await page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), "kiai-admin-preview", "students-gym-mobile.png"), FullPage = true });
        }
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Visão da área" }).GetByRole(AriaRole.Link, new() { Name = "Geral", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Aluno Sem Modalidade" }).WaitForAsync();
        Assert.Contains("Aluno Fight", await page.Locator("body").InnerTextAsync());
        Assert.Contains("Aluno Gym", await page.Locator("body").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=window.innerWidth"));
        if (role == "owner")
        {
            await page.SetViewportSizeAsync(1440, 1000);
            await page.ScreenshotAsync(new() { Path = Path.Combine(Path.GetTempPath(), "kiai-admin-preview", "students-general-desktop.png"), FullPage = true });
            await page.SetViewportSizeAsync(390, 844);
        }
        if (role != "reception")
        {
            await page.GotoAsync(env.AdminUrl + "/gym");
            await page.Locator("a.workspace-card").Filter(new() { HasText = "Metas e progresso" }).ClickAsync();
            await page.WaitForURLAsync(env.AdminUrl + "/fitness?area=gym");
            await page.GetByLabel("Filtrar por aluno", new() { Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Option, new() { Name = "Aluno Gym", Exact = true }).WaitForAsync();
            Assert.Equal(0, await page.GetByRole(AriaRole.Option, new() { Name = "Aluno Fight", Exact = true }).CountAsync());
        }
    }

    [Fact]
    public async Task LessonsPage_ShouldLoadLocalDateFiltersInSaoPauloTimezone()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync(timezone: "America/Sao_Paulo");
        using var owner = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        await SignInAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var modality = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Aula no fuso de São Paulo", Type = "martial_art" });
        var localNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "America/Sao_Paulo");
        var from = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, localNow.Offset).AddDays(2);
        var lesson = await CreateAsync<LessonResponse>(owner, "lessons", new { ModalityId = modality.Id, StartsAt = from.AddHours(12), EndsAt = from.AddHours(13) });
        var query = $"catalog/lessons?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(from.AddDays(1).ToString("O"))}";
        var agenda = await owner.GetFromJsonAsync<PagedResponse<LessonResponse>>(query);
        Assert.Equal(lesson.Id, Assert.Single(agenda!.Items).Id);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { TimezoneId = "America/Sao_Paulo" });
        await LoginPageAsync(page, env.AdminUrl, env.OwnerEmail, env.OwnerPassword);
        await page.WaitForURLAsync(env.AdminUrl + "/");
        await page.GotoAsync(env.AdminUrl + "/lessons");
        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = modality.Name });
        await row.WaitForAsync();
        Assert.Contains("12:00 - 13:00", await row.InnerTextAsync());
        Assert.DoesNotContain("Não foi possível carregar as aulas", await page.Locator("body").InnerTextAsync());
    }

    [Fact]
    public async Task ExistingPeopleCanBeLinkedFromBothRegistrationFormsWithoutDuplicatingIdentity()
    {
        await using var env=new E2EEnvironment();await env.StartAsync();using var owner=new HttpClient{BaseAddress=new Uri(env.ApiUrl+"/api/v1/")};await SignInAsync(owner,env.OwnerEmail,env.OwnerPassword);
        var client=await CreateAsync<ClientResponse>(owner,"clients",new{FullName="Pessoa com dois vínculos",BirthDate="1995-04-12"});
        var staff=await CreateAsync<System.Text.Json.JsonElement>(owner,"people",new{FullName="Equipe que também treina"});var staffId=staff.GetProperty("personId").GetGuid();
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});var page=await browser.NewPageAsync();await LoginPageAsync(page,env.AdminUrl,env.OwnerEmail,env.OwnerPassword);await page.WaitForURLAsync(env.AdminUrl+"/");
        await page.GotoAsync(env.AdminUrl+"/teachers");await page.GetByRole(AriaRole.Button,new(){Name="Novo professor",Exact=true}).ClickAsync();var dialog=page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Pessoa já cadastrada (opcional)").ClickAsync();await page.GetByText("Pessoa com dois vínculos",new(){Exact=true}).Last.ClickAsync();await dialog.GetByRole(AriaRole.Button,new(){Name="Salvar",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Row).Filter(new(){HasText="Pessoa com dois vínculos"}).WaitForAsync();
        var teachers=await owner.GetFromJsonAsync<PagedResponse<KiaiControl.Contracts.Teachers.TeacherResponse>>("catalog/teachers?search=Pessoa");var teacher=Assert.Single(teachers!.Items);Assert.Equal(client.PersonId,teacher.PersonId);Assert.Equal(new DateOnly(1995,4,12),teacher.BirthDate);
        await page.GotoAsync(env.AdminUrl+"/clients");await page.GetByRole(AriaRole.Button,new(){Name="Novo aluno",Exact=true}).ClickAsync();dialog=page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Pessoa já cadastrada (opcional)").ClickAsync();await page.GetByText("Equipe que também treina",new(){Exact=true}).Last.ClickAsync();await dialog.GetByRole(AriaRole.Button,new(){Name="Salvar",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Row).Filter(new(){HasText="Equipe que também treina"}).WaitForAsync();
        var students=await owner.GetFromJsonAsync<PagedResponse<ClientResponse>>("catalog/clients?search=Equipe");Assert.Equal(staffId,Assert.Single(students!.Items).PersonId);
    }

    [Fact]
    public async Task OwnerCanManageStaffAndSeeDistinctGymAndFightWorkspaces()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var http = new HttpClient
        {
            BaseAddress = new Uri(env.ApiUrl + "/api/v1/")
        };
        var owner = await SignInAsync(http, env.OwnerEmail, env.OwnerPassword);
        await CreateAsync<ModalityResponse>(http, "modalities", new { Name = "Jiu-jitsu", Type = "martial_art" });
        await CreateAsync<ModalityResponse>(http, "modalities", new { Name = "Musculação", Type = "fitness" });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
        await LoginPageAsync(page, env.AdminUrl, env.OwnerEmail, env.OwnerPassword);
        await page.WaitForURLAsync(env.AdminUrl + "/");
        await page.GotoAsync(env.AdminUrl + "/people");
        await page.GetByRole(AriaRole.Button, new() { Name = "Nova pessoa", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        var staffEmail = $"staff-{Guid.NewGuid():N}@example.test";
        await dialog.GetByLabel("Nome completo").FillAsync("Recepção compartilhada");
        await dialog.GetByLabel("E-mail de contato").FillAsync(staffEmail);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Salvar pessoa" }).ClickAsync();
        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Recepção compartilhada" });
        await row.GetByRole(AriaRole.Button, new() { Name = "Gerenciar", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog);
        await dialog.GetByLabel("Senha temporária").FillAsync(Temporary);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Criar acesso", Exact = true }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Bloquear acesso" }).WaitForAsync();
        using var staff = new HttpClient
        {
            BaseAddress = http.BaseAddress
        };
        await SignInAsync(staff, staffEmail, Temporary);
        using (var change = await staff.PostAsJsonAsync("auth/change-password", new { CurrentPassword = Temporary, NewPassword = Personal }))
            change.EnsureSuccessStatusCode();
        await SignInAsync(staff, staffEmail, Personal);
        using (var allowed = await staff.GetAsync("catalog/clients"))
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Bloquear acesso" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Reativar acesso" }).WaitForAsync();
        using (var denied = await staff.GetAsync("catalog/clients"))
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Reativar acesso" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Bloquear acesso" }).WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Concluir" }).ClickAsync();
        using (var revoke = await http.PutAsJsonAsync("access/roles/reception/permissions", new { PermissionCode = PermissionCodes.ClientsRead }))
            revoke.EnsureSuccessStatusCode();
        using (var revoke = await http.DeleteAsync("access/roles/reception/permissions/clients.manage"))
            revoke.EnsureSuccessStatusCode();
        await SignInAsync(staff, staffEmail, Personal);
        using (var read = await staff.GetAsync("catalog/clients"))
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using (var write = await staff.PostAsJsonAsync("clients", new { FullName = "Não pode criar" }))
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        using (var self = await http.PutAsJsonAsync($"people/{owner.User.PersonId}/account/state", new { IsActive = false }))
            Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        var shots = Path.Combine(Path.GetTempPath(), "kiai-admin-preview");
        Directory.CreateDirectory(shots);
        foreach (var area in new[]
        {
            "fight",
            "gym"
        }

        )
        {
            await page.GotoAsync(env.AdminUrl + "/" + area);
            await page.GetByRole(AriaRole.Heading, new() { Name = area == "fight" ? "Kiai Fight" : "Kiai Gym", Exact = true }).WaitForAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, area + "-desktop.png"), FullPage = true });
            await page.SetViewportSizeAsync(390, 844);
            await page.WaitForFunctionAsync("document.documentElement.scrollWidth<=window.innerWidth && document.querySelector('.workspace-hero').getBoundingClientRect().left<=24");
            await page.ScreenshotAsync(new() { Path = Path.Combine(shots, area + "-mobile.png"), FullPage = true });
            await page.SetViewportSizeAsync(1440, 1000);
        }

        var staffPage = await browser.NewPageAsync();
        await LoginPageAsync(staffPage, env.AdminUrl, staffEmail, Personal);
        await staffPage.WaitForURLAsync(env.AdminUrl + "/clients");
        Assert.Equal(0, await staffPage.GetByRole(AriaRole.Button, new() { Name = "Novo aluno", Exact = true }).CountAsync());
    }

    [Fact]
    public async Task StudentCanChangeTemporaryPasswordCheckInAndReadOnlyTheirOwnJourneyAndBilling()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var owner = new HttpClient
        {
            BaseAddress = new Uri(env.ApiUrl + "/api/v1/")
        };
        await SignInAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var email = $"student-{Guid.NewGuid():N}@example.test";
        var student = await CreateAsync<ClientResponse>(owner, "clients", new { FullName = "Aluno Portal", Email = email });
        var other = await CreateAsync<ClientResponse>(owner, "clients", new { FullName = "Outro aluno" });
        await CreateAsync<UserAccountResponse>(owner, $"access/people/{student.PersonId}/account", new { Email = email, TemporaryPassword = Temporary, RoleCode = "client" });
        var modality = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Jiu-jitsu Portal", Type = "martial_art" });
        using (var linked = await owner.PostAsync($"clients/{student.Id}/modalities/{modality.Id}", null))
            linked.EnsureSuccessStatusCode();
        var lesson = await CreateAsync<LessonResponse>(owner, "lessons", new { ModalityId = modality.Id, StartsAt = DateTimeOffset.UtcNow.AddDays(2), EndsAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(1) });
        using (var settings = await owner.PutAsJsonAsync("organizations/settings", new { Timezone = "America/Sao_Paulo", CurrencyCode = "BRL", AllowClientCheckin = true, RequireTeacherConfirmation = true, DefaultLateToleranceMinutes = 10 }))
            settings.EnsureSuccessStatusCode();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reference = new DateOnly(today.Year, today.Month, 1);
        foreach (var pair in new[]
        {
            (student.Id, 123.45m),
            (other.Id, 987.65m)
        }

        )
        {
            var plan = await CreateAsync<BillingPlanResponse>(owner, "billing/plans", new { Name = "Plano " + pair.Id, MonthlyAmount = pair.Item2, DueDay = 10 });
            await CreateAsync<ClientPlanSubscriptionResponse>(owner, $"billing/clients/{pair.Id}/subscriptions", new { PlanId = plan.Id, StartsAt = reference });
        }

        await CreateAsync<List<BillingResponse>>(owner, "billing/invoices/generate", new { ReferenceMonth = reference });
        var goal = await CreateAsync<FitnessGoalResponse>(owner, "fitness-goals", new { ClientId = student.Id, Title = "Meta pessoal", GoalType = "weight", Direction = "decrease", InitialValue = 80, TargetValue = 70, Unit = "kg", StartsAt = today, DueDate = today.AddDays(30) });
        var otherGoal = await CreateAsync<FitnessGoalResponse>(owner, "fitness-goals", new { ClientId = other.Id, Title = "Meta privada", GoalType = "weight", TargetValue = 90, Unit = "kg", StartsAt = today, DueDate = today.AddDays(30) });
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await LoginPageAsync(page, env.AdminUrl, email, Temporary);
        await page.WaitForURLAsync(env.AdminUrl + "/change-password");
        await page.GetByLabel("Senha temporária atual", new() { Exact = true }).FillAsync(Temporary);
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync("abcdef");
        await page.GetByLabel("Confirme a nova senha").FillAsync("abcdef");
        await page.GetByRole(AriaRole.Button, new() { Name = "Alterar senha", Exact = true }).ClickAsync();
        await page.Locator(".validation-errors").GetByText(PasswordPolicy.Requirements, new() { Exact = true }).WaitForAsync();
        Assert.EndsWith("/change-password", page.Url);
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync(Personal);
        await page.GetByLabel("Confirme a nova senha").FillAsync(Personal);
        await page.GetByRole(AriaRole.Button, new() { Name = "Alterar senha", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/login");
        await LoginPageAsync(page, env.AdminUrl, email, Personal);
        await page.WaitForURLAsync(env.AdminUrl + "/student");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fazer check-in", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancelar check-in", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancelar check-in", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Fazer check-in", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Minha evolução", Exact = true }).ClickAsync();
        await page.GetByText("Presenças confirmadas", new() { Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Gym · metas", Exact = true }).ClickAsync();
        await page.GetByText("Meta pessoal", new() { Exact = false }).WaitForAsync();
        Assert.DoesNotContain("Meta privada", await page.Locator("body").InnerTextAsync());
        await page.GetByRole(AriaRole.Tab, new() { Name = "Meu financeiro", Exact = true }).ClickAsync();
        await page.GetByText("R$ 123,45", new() { Exact = true }).WaitForAsync();
        Assert.DoesNotContain("987,65", await page.Locator("body").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=window.innerWidth"));
        using var personal = new HttpClient
        {
            BaseAddress = owner.BaseAddress
        };
        await SignInAsync(personal, email, Personal);
        using (var forbidden = await personal.GetAsync("billing"))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using (var hidden = await personal.GetAsync($"portal/goals/{otherGoal.Id}/progress"))
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var invoices = await personal.GetFromJsonAsync<List<BillingResponse>>($"portal/invoices?clientId={other.Id}");
        Assert.Equal(student.Id, Assert.Single(invoices!).ClientId);
        await env.RemoveOrganizationSettingsAsync();
        var legacyAgenda = await personal.GetFromJsonAsync<List<ClientPortalLessonResponse>>("portal/lessons");
        var legacyLesson = Assert.Single(legacyAgenda!);
        Assert.Equal(lesson.Id, legacyLesson.Id);
        Assert.False(legacyLesson.AllowCheckIn);
        await page.GotoAsync(env.AdminUrl + "/student");
        await page.GetByText("Presença registrada pela academia", new() { Exact = true }).WaitForAsync();
        Assert.DoesNotContain("Não foi possível carregar sua agenda", await page.Locator("body").InnerTextAsync());
        await page.GotoAsync(env.AdminUrl + "/clients");
        await page.GetByText("Acesso restrito", new() { Exact = true }).WaitForAsync();
        Assert.Equal(0, await page.GetByRole(AriaRole.Button, new() { Name = "Novo aluno", Exact = true }).CountAsync());
    }

    [Fact]
    public async Task TeacherScopeAndFinanceRole_ShouldWorkWithoutUnrelatedManagementPermissions()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var owner = new HttpClient
        {
            BaseAddress = new Uri(env.ApiUrl + "/api/v1/")
        };
        await SignInAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var teacherEmail = $"teacher-{Guid.NewGuid():N}@example.test";
        var teacher = await CreateAsync<KiaiControl.Contracts.Teachers.TeacherResponse>(owner, "teachers", new { FullName = "Professor exclusivo", Email = teacherEmail });
        var otherTeacher = await CreateAsync<KiaiControl.Contracts.Teachers.TeacherResponse>(owner, "teachers", new { FullName = "Outro professor" });
        await CreateAsync<UserAccountResponse>(owner, $"access/people/{teacher.PersonId}/account", new { Email = teacherEmail, TemporaryPassword = Temporary, RoleCode = "teacher" });
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var lessons = new List<LessonResponse>();
        foreach (var id in new[]
        {
            teacher.Id,
            otherTeacher.Id
        }

        )
        {
            var modality = await CreateAsync<ModalityResponse>(owner, "modalities", new { Name = "Aula " + id, Type = "martial_art" });
            await CreateAsync<ModalityScheduleResponse>(owner, $"modalities/{modality.Id}/schedules", new { TeacherId = id, DayOfWeek = (int)day.DayOfWeek, StartsAt = "18:00:00", EndsAt = "19:00:00" });
            lessons.AddRange(await CreateAsync<List<LessonResponse>>(owner, "lessons/generate-from-modality", new { ModalityId = modality.Id, StartDate = day, EndDate = day }));
        }

        using var personal = new HttpClient
        {
            BaseAddress = owner.BaseAddress
        };
        await SignInAsync(personal, teacherEmail, Temporary);
        using (var change = await personal.PostAsJsonAsync("auth/change-password", new { CurrentPassword = Temporary, NewPassword = Personal }))
            change.EnsureSuccessStatusCode();
        await SignInAsync(personal, teacherEmail, Personal);
        var agenda = await personal.GetFromJsonAsync<List<LessonResponse>>("lessons");
        Assert.Equal(lessons[0].Id, Assert.Single(agenda!).Id);
        using (var forbidden = await personal.GetAsync($"lessons/{lessons[1].Id}/attendances"))
            Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        using (var pending = await personal.GetAsync("attendance/pending"))
            Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
        using (var forbidden = await personal.PostAsJsonAsync("lessons", new { ModalityId = lessons[0].ModalityId, StartsAt = DateTimeOffset.UtcNow.AddDays(4), EndsAt = DateTimeOffset.UtcNow.AddDays(4).AddHours(1) }))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var person = await CreateAsync<System.Text.Json.JsonElement>(owner, "people", new { FullName = "Financeiro dedicado" });
        var personId = person.GetProperty("personId").GetGuid();
        var financeEmail = $"finance-{Guid.NewGuid():N}@example.test";
        await CreateAsync<UserAccountResponse>(owner, $"access/people/{personId}/account", new { Email = financeEmail, TemporaryPassword = Temporary, RoleCode = "financial" });
        using var finance = new HttpClient
        {
            BaseAddress = owner.BaseAddress
        };
        await SignInAsync(finance, financeEmail, Temporary);
        using (var change = await finance.PostAsJsonAsync("auth/change-password", new { CurrentPassword = Temporary, NewPassword = Personal }))
            change.EnsureSuccessStatusCode();
        await SignInAsync(finance, financeEmail, Personal);
        using (var options = await finance.GetAsync("billing/options"))
            Assert.Equal(HttpStatusCode.OK, options.StatusCode);
        using (var clients = await finance.GetAsync("clients"))
            Assert.Equal(HttpStatusCode.Forbidden, clients.StatusCode);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await LoginPageAsync(page, env.AdminUrl, financeEmail, Personal);
        await page.WaitForURLAsync(env.AdminUrl + "/");
        await page.GotoAsync(env.AdminUrl + "/billing");
        await page.GetByRole(AriaRole.Tab, new() { Name = "Planos", Exact = true }).WaitForAsync();
        Assert.DoesNotContain("Seu perfil não tem permissão", await page.Locator("body").InnerTextAsync());
    }

    [Fact]
    public async Task PasswordRecovery_ShouldConsumeFragmentTokenAndInvalidatePreviousSessions()
    {
        await using var env = new E2EEnvironment();
        await env.StartAsync();
        using var owner = new HttpClient
        {
            BaseAddress = new Uri(env.ApiUrl + "/api/v1/")
        };
        await SignInAsync(owner, env.OwnerEmail, env.OwnerPassword);
        var person = await CreateAsync<System.Text.Json.JsonElement>(owner, "people", new { FullName = "Conta de recuperação" });
        var personId = person.GetProperty("personId").GetGuid();
        var email = $"recover-{Guid.NewGuid():N}@example.test";
        await CreateAsync<UserAccountResponse>(owner, $"access/people/{personId}/account", new { Email = email, TemporaryPassword = Temporary, RoleCode = "reception" });
        using var account = new HttpClient
        {
            BaseAddress = owner.BaseAddress
        };
        await SignInAsync(account, email, Temporary);
        using (var change = await account.PostAsJsonAsync("auth/change-password", new { CurrentPassword = Temporary, NewPassword = Personal }))
            change.EnsureSuccessStatusCode();
        var old = await SignInAsync(account, email, Personal);
        using var anonymous = new HttpClient
        {
            BaseAddress = owner.BaseAddress
        };
        var existing = await CreateAsync<ForgotPasswordResponse>(anonymous, "auth/forgot-password", new { Email = email });
        var unknown = await CreateAsync<ForgotPasswordResponse>(anonymous, "auth/forgot-password", new { Email = "unknown@example.test" });
        Assert.Equal(existing.Message, unknown.Message);
        var token = await env.IssuePasswordResetTokenAsync(email);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(env.AdminUrl + "/reset-password#token=" + Uri.EscapeDataString(token));
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync("Aa1234");
        await page.GetByLabel("Confirme a nova senha").FillAsync("Aa1234");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar nova senha", Exact = true }).ClickAsync();
        await page.Locator(".mud-alert").GetByText(PasswordPolicy.Requirements, new() { Exact = true }).WaitForAsync();
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync("Aa!cde");
        await page.GetByLabel("Confirme a nova senha").FillAsync("Aa!cde");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar nova senha", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/login");
        using (var revoked = await account.GetAsync("auth/me"))
            Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using (var revoked = await anonymous.PostAsJsonAsync("auth/refresh", new { RefreshToken = old.RefreshToken }))
            Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        await LoginPageAsync(page, env.AdminUrl, email, "Aa!cde");
        await page.WaitForURLAsync(env.AdminUrl + "/clients");
        await page.GotoAsync(env.AdminUrl + "/account");
        await page.GetByLabel("Senha atual", new() { Exact = true }).FillAsync("Aa!cde");
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync("Abc!fg");
        await page.GetByLabel("Confirme a nova senha").FillAsync("Abc!fg");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar nova senha", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/login");
        await LoginPageAsync(page, env.AdminUrl, email, "Abc!fg");
        await page.WaitForURLAsync(env.AdminUrl + "/clients");
        using (var used = await anonymous.PostAsJsonAsync("auth/reset-password", new { Token = token, NewPassword = Personal }))
            Assert.Equal(HttpStatusCode.Unauthorized, used.StatusCode);
    }
}
