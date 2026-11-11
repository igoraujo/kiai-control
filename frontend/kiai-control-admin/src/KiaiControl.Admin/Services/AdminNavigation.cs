using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.Services;
public static class AdminNavigation
{
    public static string InArea(string path, string area)
    {
        var selected = AcademyArea.Normalize(area);
        return selected == AcademyArea.General ? path : $"{path}?area={selected}";
    }

    public static bool HasRole(AuthUserProfileResponse? user, string role) => user?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) == true;
    public static bool CanUse(AuthUserProfileResponse? user, string permission) => user?.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase) == true && user.Roles.Any(role => PermissionRoleRules.CanGrant(role, permission));
    public static bool CanRead(AuthUserProfileResponse? user, string permission) => CanUse(user, permission) || (PermissionCodes.ReadPermissions.TryGetValue(permission, out var read) && CanUse(user, read));
    public static string Home(AuthUserProfileResponse? user)
    {
        if (user is null)
            return "/login";
        if (user.MustChangePassword)
            return "/change-password";
        if (HasRole(user, "platform_owner"))
            return "/platform/organizations";
        if (CanUse(user, PermissionCodes.ReportsRead))
            return "/";
        if (HasRole(user, "teacher"))
            return "/teacher";
        if (CanRead(user, PermissionCodes.ClientsManage))
            return "/clients";
        if (CanUse(user, PermissionCodes.BillingManage))
            return "/billing";
        if (HasRole(user, "client"))
            return "/student";
        return "/welcome";
    }

    public static bool CanVisit(AuthUserProfileResponse? user, string path, KiaiControl.Contracts.Modalities.WorkspaceCapabilitiesResponse? capabilities=null)
    {
        path = "/" + path.Trim('/').ToLowerInvariant();
        if (path is "/login" or "/forgot-password" or "/reset-password" or "/error" or "/not-found")
            return true;
        if (user is null)
            return false;
        if (user.MustChangePassword)
            return path == "/change-password";
        if (path is "/account" or "/welcome" or "/access-denied" or "/change-password")
            return true;
        if (path.StartsWith("/platform/"))
            return HasRole(user, "platform_owner");
        if (path.StartsWith("/student"))
            return HasRole(user, "client");
        if (path.StartsWith("/teacher"))
            return path == "/teacher" ? HasRole(user, "teacher") : CanRead(user, PermissionCodes.TeachersManage);
        if (path == "/fight")
            return capabilities?.HasMartialArts!=false && (CanRead(user,PermissionCodes.ClientsManage)||CanUse(user,PermissionCodes.GraduationManage)||CanUse(user,PermissionCodes.LessonsManage)||CanUse(user,PermissionCodes.RankingManage));
        if (path == "/gym")
            return capabilities?.HasFitnessActivities!=false && (CanUse(user,PermissionCodes.FitnessManage)||CanUse(user,PermissionCodes.LessonsManage)||CanRead(user,PermissionCodes.ClientsManage));
        if(path=="/graduation"&&capabilities?.HasGraduation==false)return false;

        var permission = path.Split('/')[1] switch
        {
            "" => PermissionCodes.ReportsRead,
            "clients" => PermissionCodes.ClientsManage,
            "teachers" => PermissionCodes.TeachersManage,
            "branches" => PermissionCodes.BranchesManage,
            "modalities" => PermissionCodes.ModalitiesManage,
            "billing" => PermissionCodes.BillingManage,
            "lessons" => PermissionCodes.LessonsManage,
            "attendance" => PermissionCodes.AttendanceManage,
            "graduation" => PermissionCodes.GraduationManage,
            "fitness" => PermissionCodes.FitnessManage,
            "rankings" => PermissionCodes.RankingManage,
            "notifications" => PermissionCodes.NotificationsRead,
            "audit" => PermissionCodes.AuditRead,
            "people" or "access" => PermissionCodes.AccessManage,
            "organization" => PermissionCodes.OrganizationsManage,
            _ => null
        };
        return permission is not null && CanRead(user, permission);
    }

    public static string RoleLabel(string role) => role switch
    {
        "owner" => "Responsável",
        "admin" => "Administrador",
        "teacher" => "Professor",
        "client" => "Aluno",
        "reception" => "Recepção",
        "financial" => "Financeiro",
        "platform_owner" => "Dono da plataforma",
        _ => role
    };
}
