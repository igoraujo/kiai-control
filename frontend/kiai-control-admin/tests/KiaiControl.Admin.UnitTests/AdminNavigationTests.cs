using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Fitness;

namespace KiaiControl.Admin.UnitTests;
public sealed class AdminNavigationTests
{
    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("reception")]
    [InlineData("financial")]
    [InlineData("client")]
    [InlineData("teacher")]
    public void TenantRoles_ShouldNeverOpenPlatformPagesEvenWithAllPermissionClaims(string role)
    {
        var user = User(role, PermissionCodes.All);
        foreach (var path in new[] { "/platform/organizations", "/platform/organizations/00000000-0000-0000-0000-000000000001", "/platform/audit", "/platform/operations" })
            Assert.False(AdminNavigation.CanVisit(user, path));
    }

    [Fact]
    public void PlatformRole_ShouldLandInDedicatedAreaWithoutTenantPermissions()
    {
        var user = User("platform_owner");
        Assert.Equal("/platform/organizations", AdminNavigation.Home(user));
        Assert.True(AdminNavigation.CanVisit(user, "/platform/audit"));
        Assert.True(AdminNavigation.CanVisit(user, "/platform/operations"));
        Assert.False(AdminNavigation.CanVisit(user, "/clients"));
        Assert.False(AdminNavigation.CanVisit(user, "/fight"));
        Assert.False(AdminNavigation.CanVisit(user, "/gym"));
    }
    private static AuthUserProfileResponse User(string role, params string[] permissions) => new()
    {
        Roles = [role],
        Permissions = permissions
    };
    [Fact]
    public void Student_ShouldLandInOwnPortalAndNeverOpenManagementWithOnlyStudentPermissions()
    {
        var student = User("client", PermissionCodes.NotificationsRead, PermissionCodes.AttendanceCheckIn);
        Assert.Equal("/student", AdminNavigation.Home(student));
        Assert.True(AdminNavigation.CanVisit(student, "/student"));
        Assert.True(AdminNavigation.CanVisit(student, "/account"));
        Assert.False(AdminNavigation.CanVisit(student, "/clients"));
        Assert.False(AdminNavigation.CanVisit(student, "/billing"));
        Assert.False(AdminNavigation.CanVisit(student, "/"));
    }

    [Fact]
    public void ReceptionAndTeacher_ShouldLandOnAnAuthorizedPageAndRespectRoleBoundaries()
    {
        Assert.Equal("/clients", AdminNavigation.Home(User("reception", PermissionCodes.ClientsManage)));
        Assert.Equal("/teacher", AdminNavigation.Home(User("teacher", PermissionCodes.LessonsManage)));
        Assert.False(AdminNavigation.CanVisit(User("teacher", PermissionCodes.ReportsRead), "/"));
        Assert.False(PermissionRoleRules.CanGrant("teacher", PermissionCodes.ReportsRead));
    }

    [Fact]
    public void TemporaryPassword_ShouldAllowOnlyTheRequiredPasswordPage()
    {
        var user = new AuthUserProfileResponse
        {
            Roles = ["owner"],
            Permissions = PermissionCodes.All,
            MustChangePassword = true
        };
        Assert.Equal("/change-password", AdminNavigation.Home(user));
        Assert.False(AdminNavigation.CanVisit(user, "/people"));
        Assert.True(AdminNavigation.CanVisit(user, "/change-password"));
    }

    [Fact]
    public void ReducingGoal_ShouldMeasureProgressFromInitialValueTowardsLowerTarget()
    {
        Assert.Equal(50, FitnessProgress.Percentage(new FitnessGoalResponse { Direction = "decrease", InitialValue = 80, CurrentValue = 75, TargetValue = 70 }));
        Assert.Equal(0, FitnessProgress.Percentage(new FitnessGoalResponse { Direction = "decrease", InitialValue = 80, CurrentValue = 85, TargetValue = 70 }));
    }
    [Fact]
    public void HybridWorkspace_ShouldOfferBothAreasWithoutMartialGraduation()
    {
        var user=User("owner",PermissionCodes.All);
        var capabilities=new KiaiControl.Contracts.Modalities.WorkspaceCapabilitiesResponse{HasMartialArts=true,HasFitnessActivities=true,HasGraduation=false};
        Assert.True(AdminNavigation.CanVisit(user,"/fight",capabilities));
        Assert.True(AdminNavigation.CanVisit(user,"/gym",capabilities));
        Assert.False(AdminNavigation.CanVisit(user,"/graduation",capabilities));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("reception")]
    public void StudentReaders_ShouldOpenBothWorkspacesWithoutGainingOtherPermissions(string role)
    {
        var user = User(role, PermissionCodes.ClientsRead);
        Assert.True(AdminNavigation.CanVisit(user, "/fight"));
        Assert.True(AdminNavigation.CanVisit(user, "/gym"));
        Assert.True(AdminNavigation.CanVisit(user, "/clients"));
        Assert.False(AdminNavigation.CanVisit(user, "/billing"));
        Assert.False(AdminNavigation.CanVisit(user, "/lessons"));
        Assert.Equal("/clients?area=gym", AdminNavigation.InArea("/clients", "gym"));
        Assert.Equal("/clients?area=fight", AdminNavigation.InArea("/clients", "fight"));
        Assert.Equal("/clients", AdminNavigation.InArea("/clients", "shared"));
    }
}
