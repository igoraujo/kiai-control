using Bunit;
using KiaiControl.Admin.Components.Layout;
using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Modalities;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System.Net;
using System.Net.Http.Json;

namespace KiaiControl.Admin.UnitTests;

public sealed class NavMenuTests
{
    [Fact]
    public async Task NavMenu_ShouldShowOnlyLinksAllowedBySessionPermissions()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        await session.SetAsync(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "reception@example.test",
                Roles = ["reception"],
                Permissions = [PermissionCodes.ClientsManage, PermissionCodes.AuditRead]
            }
        });
        context.Services.AddSingleton(session);
        AddApiClient(context, session, new WorkspaceCapabilitiesResponse());

        var menu = context.Render<NavMenu>();
        var links = menu.FindAll("a").Select(link => link.GetAttribute("href")).ToArray();

        Assert.Contains("/clients", links);
        Assert.DoesNotContain("/branches", links);
        Assert.DoesNotContain("/teachers", links);
        Assert.DoesNotContain("/attendance/pending", links);
        Assert.DoesNotContain("/graduation", links);
        Assert.DoesNotContain("/fitness", links);
        Assert.DoesNotContain("/rankings", links);
        Assert.DoesNotContain("/notifications", links);
        Assert.DoesNotContain("/audit", links);
    }

    [Fact]
    public async Task NavMenu_ShouldShowOnlyMartialArtsAreaForMartialArtsOrganization()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        await session.SetAsync(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = [
                    PermissionCodes.GraduationManage,
                    PermissionCodes.FitnessManage,
                ]
            }
        });
        context.Services.AddSingleton(session);
        AddApiClient(context, session, new WorkspaceCapabilitiesResponse
        {
            HasMartialArts = true,
            HasGraduation = true
        });

        var menu = context.Render<NavMenu>();
        menu.WaitForAssertion(() =>
        {
            var links = menu.FindAll("a").Select(link => link.GetAttribute("href")).ToArray();
            Assert.Contains("/graduation", links);
            Assert.DoesNotContain("/fitness", links);
        });
    }

    [Fact]
    public async Task NavMenu_ShouldShowOnlyFitnessAreaForFitnessOrganization()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        await session.SetAsync(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = [PermissionCodes.GraduationManage, PermissionCodes.FitnessManage]
            }
        });
        context.Services.AddSingleton(session);
        AddApiClient(context, session, new WorkspaceCapabilitiesResponse { HasFitnessActivities = true });

        var menu = context.Render<NavMenu>();
        menu.WaitForAssertion(() =>
        {
            var links = menu.FindAll("a").Select(link => link.GetAttribute("href")).ToArray();
            Assert.DoesNotContain("/graduation", links);
            Assert.Contains("/fitness", links);
        });
    }

    [Fact]
    public async Task NavMenu_ShouldShowBothAreasForMixedOrganization()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        await session.SetAsync(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = [PermissionCodes.GraduationManage, PermissionCodes.FitnessManage]
            }
        });
        context.Services.AddSingleton(session);
        AddApiClient(context, session, new WorkspaceCapabilitiesResponse
        {
            HasMartialArts = true,
            HasFitnessActivities = true,
            HasGraduation = true
        });

        var menu = context.Render<NavMenu>();
        menu.WaitForAssertion(() =>
        {
            var links = menu.FindAll("a").Select(link => link.GetAttribute("href")).ToArray();
            Assert.Contains("/graduation", links);
            Assert.Contains("/fitness", links);
        });
    }

    [Fact]
    public async Task NavMenu_ShouldAllowEachMainSectionToCollapseIndependently()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        await session.SetAsync(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = [
                    PermissionCodes.ReportsRead,
                    PermissionCodes.ClientsManage,
                    PermissionCodes.GraduationManage,
                    PermissionCodes.LessonsManage,
                    PermissionCodes.AttendanceManage,
                    PermissionCodes.RankingManage,
                    PermissionCodes.NotificationsRead,
                    PermissionCodes.AccessPermissionsManage
                ]
            }
        });
        context.Services.AddSingleton(session);
        AddApiClient(context, session, new WorkspaceCapabilitiesResponse
        {
            HasMartialArts = true,
            HasGraduation = true
        });

        var menu = context.Render<NavMenu>();
        menu.WaitForAssertion(() =>
        {
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/");
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/clients");
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/graduation");
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/lessons");
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/rankings");
            Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/notifications");
        });

        var principal = menu.Find("button[aria-controls='menu-section-principal']");
        var management = menu.Find("button[aria-controls='menu-section-academymanagement']");
        var martialArts = menu.Find("button[aria-controls='menu-section-martialarts']");
        var routine = menu.Find("button[aria-controls='menu-section-routine']");
        var community = menu.Find("button[aria-controls='menu-section-community']");
        var administration = menu.Find("button[aria-controls='menu-section-administration']");

        principal.Click();
        management.Click();
        martialArts.Click();
        routine.Click();
        community.Click();
        administration.Click();

        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-principal']").GetAttribute("aria-expanded"));
        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-academymanagement']").GetAttribute("aria-expanded"));
        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-martialarts']").GetAttribute("aria-expanded"));
        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-routine']").GetAttribute("aria-expanded"));
        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-community']").GetAttribute("aria-expanded"));
        Assert.Equal("false", menu.Find("button[aria-controls='menu-section-administration']").GetAttribute("aria-expanded"));
        Assert.True(menu.Find("#menu-section-principal").HasAttribute("hidden"));
        Assert.True(menu.Find("#menu-section-academymanagement").HasAttribute("hidden"));
        Assert.True(menu.Find("#menu-section-martialarts").HasAttribute("hidden"));
        Assert.True(menu.Find("#menu-section-routine").HasAttribute("hidden"));
        Assert.True(menu.Find("#menu-section-community").HasAttribute("hidden"));
        Assert.True(menu.Find("#menu-section-administration").HasAttribute("hidden"));
    }

    private static void AddApiClient(
        BunitContext context,
        AdminSession session,
        WorkspaceCapabilitiesResponse capabilities)
    {
        var clientFactory = new StubHttpClientFactory(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(capabilities)
        });
        context.Services.AddSingleton<IHttpClientFactory>(clientFactory);
        context.Services.AddSingleton(new AdminApiClient(clientFactory, session));
    }

    private sealed class StubHttpClientFactory(Func<HttpResponseMessage> createResponse) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(createResponse), disposeHandler: true)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(createResponse());
    }
}