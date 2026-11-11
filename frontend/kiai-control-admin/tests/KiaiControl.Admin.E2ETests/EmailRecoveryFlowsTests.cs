using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using KiaiControl.Contracts.Auth;
using Microsoft.Playwright;

namespace KiaiControl.Admin.E2ETests;

public sealed class EmailRecoveryFlowsTests
{
    [Fact]
    public async Task ForgotPassword_ShouldSendThroughConfiguredSmtpAndReceivedLinkShouldResetPasswordAndRevokeSessions()
    {
        await using var mailpit = new ContainerBuilder("axllent/mailpit:v1.33.1")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8025)).Build();
        await mailpit.StartAsync();
        await using var env = new E2EEnvironment();
        await env.StartAsync(emailEnvironment: new Dictionary<string, string>
        {
            ["Email__Provider"] = "Smtp", ["Email__FromAddress"] = "no-reply@example.test",
            ["Email__Smtp__Host"] = mailpit.Hostname, ["Email__Smtp__Port"] = mailpit.GetMappedPublicPort(1025).ToString(),
            ["Email__Smtp__Security"] = "None", ["Email__Smtp__Username"] = "", ["Email__Smtp__Password"] = "",
            ["PasswordRecovery__ResetUrl"] = "https://admin.example.test/reset-password"
        });
        using var api = new HttpClient { BaseAddress = new Uri(env.ApiUrl + "/api/v1/") };
        using var loginResponse = await api.PostAsJsonAsync("auth/login", new { email = env.OwnerEmail, password = env.OwnerPassword });
        loginResponse.EnsureSuccessStatusCode();
        var oldSession = (await loginResponse.Content.ReadFromJsonAsync<SessionTokenResponse>())!;
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldSession.AccessToken);
        using var anonymous = new HttpClient { BaseAddress = api.BaseAddress };
        using var requested = await anonymous.PostAsJsonAsync("auth/forgot-password", new { email = env.OwnerEmail });
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        var response = await requested.Content.ReadAsStringAsync();
        using var unknown = await anonymous.PostAsJsonAsync("auth/forgot-password", new { email = "unknown@example.test" });
        Assert.Equal(response, await unknown.Content.ReadAsStringAsync());
        using var mailApi = new HttpClient { BaseAddress = new Uri($"http://{mailpit.Hostname}:{mailpit.GetMappedPublicPort(8025)}") };
        var listing = await mailApi.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        var id = Assert.Single(listing.GetProperty("messages").EnumerateArray()).GetProperty("ID").GetString();
        var received = await mailApi.GetFromJsonAsync<JsonElement>($"/api/v1/message/{id}");
        Assert.Equal(env.OwnerEmail, received.GetProperty("To")[0].GetProperty("Address").GetString());
        Assert.Equal("no-reply@example.test", received.GetProperty("From").GetProperty("Address").GetString());
        var link = new Uri(received.GetProperty("Text").GetString()!.Split('\n').Single(line => line.StartsWith("https://", StringComparison.Ordinal)).Trim());
        var token = Uri.UnescapeDataString(link.Fragment["#token=".Length..]);
        Assert.DoesNotContain(token, response);
        Assert.Empty(link.Query);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync($"{env.AdminUrl}/reset-password#token={Uri.EscapeDataString(token)}");
        await page.GetByLabel("Nova senha", new() { Exact = true }).FillAsync("Return!123");
        await page.GetByLabel("Confirme a nova senha", new() { Exact = true }).FillAsync("Return!123");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar nova senha", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(env.AdminUrl + "/login");
        using (var revoked = await api.GetAsync("auth/me")) Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using (var revoked = await anonymous.PostAsJsonAsync("auth/refresh", new { refreshToken = oldSession.RefreshToken })) Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var newLogin = await anonymous.PostAsJsonAsync("auth/login", new { email = env.OwnerEmail, password = "Return!123" });
        newLogin.EnsureSuccessStatusCode();
        using var repeated = await anonymous.PostAsJsonAsync("auth/reset-password", new { token, newPassword = "Another!123" });
        Assert.Equal(HttpStatusCode.Unauthorized, repeated.StatusCode);
    }
}
