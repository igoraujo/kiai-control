namespace KiaiControl.Admin.Components.Common;

public static class PersonStatus
{
    public static readonly string[] All = ["active", "inactive", "suspended", "canceled", "trial"];

    public static string Label(string status) => status switch
    {
        "active" => "Ativo",
        "inactive" => "Inativo",
        "suspended" => "Suspenso",
        "canceled" => "Cancelado",
        "trial" => "Experimental",
        "archived" => "Arquivada",
        _ => status
    };

    public static MudBlazor.Color Color(string status) => status switch
    {
        "active" => MudBlazor.Color.Success,
        "trial" => MudBlazor.Color.Info,
        "suspended" => MudBlazor.Color.Warning,
        "canceled" => MudBlazor.Color.Error,
        _ => MudBlazor.Color.Default
    };
}
