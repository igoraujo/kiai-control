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
        session.Set(new SessionTokenResponse
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
                Permissions = [PermissionCodes.ClientsManage]
            }
        });
        context.Services.AddSingleton(session);

        var menu = context.Render<NavMenu>();
        var links = menu.FindAll("a").Select(link => link.GetAttribute("href")).ToArray();

        Assert.Contains("/clients", links);
        Assert.DoesNotContain("/branches", links);
        Assert.DoesNotContain("/teachers", links);
        Assert.DoesNotContain("/attendance/pending", links);
    }
}