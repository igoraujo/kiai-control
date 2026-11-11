namespace KiaiControl.Contracts.Auth;
public static class PermissionRoleRules
{
    private static readonly Dictionary<string, string[]> PermissionRoles = new()
    {
        [PermissionCodes.ClientsRead] = ["owner", "admin", "reception"],
        [PermissionCodes.TeachersRead] = ["owner", "admin"],
        [PermissionCodes.BranchesRead] = ["owner", "admin", "reception"],
        [PermissionCodes.ModalitiesRead] = ["owner", "admin"],
        [PermissionCodes.AccessManage] = ["owner", "admin"],
        [PermissionCodes.AccessPermissionsManage] = ["owner"],
        [PermissionCodes.AuditRead] = ["owner", "admin"],
        [PermissionCodes.ClientsManage] = ["owner", "admin", "reception"],
        [PermissionCodes.TeachersManage] = ["owner", "admin"],
        [PermissionCodes.BranchesManage] = ["owner", "admin", "reception"],
        [PermissionCodes.ModalitiesManage] = ["owner", "admin"],
        [PermissionCodes.BillingManage] = ["owner", "admin", "financial"],
        [PermissionCodes.ReportsRead] = ["owner", "admin", "financial"],
        [PermissionCodes.OrganizationsManage] = ["owner", "admin"],
        [PermissionCodes.AttendanceManage] = ["owner", "admin", "teacher", "reception"],
        [PermissionCodes.LessonsManage] = ["owner", "admin", "teacher", "reception"],
        [PermissionCodes.GraduationManage] = ["owner", "admin", "teacher"],
        [PermissionCodes.FitnessManage] = ["owner", "admin", "teacher"],
        [PermissionCodes.RankingManage] = ["owner", "admin", "teacher"],
        [PermissionCodes.NotificationsManage] = ["owner", "admin", "teacher", "reception"]
    };
    public static bool CanGrant(string role, string permission) => PermissionCodes.All.Contains(permission) && (!PermissionRoles.TryGetValue(permission, out var roles) || roles.Contains(role, StringComparer.OrdinalIgnoreCase));
}
