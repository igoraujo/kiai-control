namespace KiaiControl.Admin.Components.Pages.Platform;

public static class PlatformLabels
{
    public static string Status(string value) => value switch
    {
        "active" => "Ativa", "inactive" => "Inativa", "suspended" => "Suspensa", _ => value
    };
    public static string Category(string value) => value switch
    {
        "request_failure" => "Falha da API", "authentication_failure" => "Falha de login",
        "audit_failure" => "Falha de auditoria", _ => value
    };
}
