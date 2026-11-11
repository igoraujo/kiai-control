namespace KiaiControl.Admin.Components.Common;
public partial class WorkspaceCard
{
    [Parameter]
    public string Href { get; set; } = "/";

    [Parameter]
    public string Area { get; set; } = "shared";

    [Parameter]
    public string Title { get; set; } = "";

    [Parameter]
    public string Description { get; set; } = "";

    [Parameter]
    public string Icon { get; set; } = Icons.Material.Filled.Dashboard;
}
