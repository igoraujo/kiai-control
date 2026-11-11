namespace KiaiControl.Admin.Components.Pages.Workspaces;

public partial class Workspace
{
    [Parameter, EditorRequired]
    public string Area { get; set; } = default!;

    private bool _fight => Area == AcademyArea.Fight;
    private string AreaLink(string path) => AdminNavigation.InArea(path, Area);
    private bool Can(string p) => AdminNavigation.CanRead(Session.Current?.User, p);

    protected override void OnParametersSet() => Session.SelectArea(Area);
}
