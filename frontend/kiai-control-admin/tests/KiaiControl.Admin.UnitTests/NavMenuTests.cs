using Bunit;
using KiaiControl.Admin.Components.Layout;
using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

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
    public async Task NavMenu_ShouldShowGraduationLinkWhenPermissionIsGranted()
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
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = [
                    PermissionCodes.GraduationManage,
                    PermissionCodes.FitnessManage,
                    PermissionCodes.RankingManage,
                    PermissionCodes.NotificationsRead,
                    PermissionCodes.AuditRead
                ]
            }
        });
        context.Services.AddSingleton(session);

        var menu = context.Render<NavMenu>();

        Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/graduation");
        Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/fitness");
        Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/rankings");
        Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/notifications");
        Assert.Contains(menu.FindAll("a"), link => link.GetAttribute("href") == "/audit");
    }
}