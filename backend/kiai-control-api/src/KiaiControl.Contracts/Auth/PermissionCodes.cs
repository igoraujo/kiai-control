namespace KiaiControl.Contracts.Auth;

public static class PermissionCodes
{
    public const string ClaimType = "permission";
    public const string AccessManage = "access.manage";
    public const string AccessPermissionsManage = "access.permissions.manage";
    public const string AuditRead = "audit.read";
    public const string AttendanceManage = "attendance.manage";
    public const string BillingManage = "billing.manage";
    public const string BranchesManage = "branches.manage";
    public const string ClassGroupsManage = "class-groups.manage";
    public const string ClientsManage = "clients.manage";
    public const string FitnessManage = "fitness.manage";
    public const string GraduationManage = "graduation.manage";
    public const string LessonsManage = "lessons.manage";
    public const string ModalitiesManage = "modalities.manage";
    public const string NotificationsManage = "notifications.manage";
    public const string OrganizationsManage = "organizations.manage";
    public const string RankingManage = "ranking.manage";
    public const string ReportsRead = "reports.read";
    public const string TeachersManage = "teachers.manage";

    public static readonly string[] All =
    [
        AccessManage,
        AccessPermissionsManage,
        AuditRead,
        AttendanceManage,
        BillingManage,
        BranchesManage,
        ClassGroupsManage,
        ClientsManage,
        FitnessManage,
        GraduationManage,
        LessonsManage,
        ModalitiesManage,
        NotificationsManage,
        OrganizationsManage,
        RankingManage,
        ReportsRead,
        TeachersManage
    ];
}
