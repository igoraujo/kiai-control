namespace KiaiControl.Admin.Components.Pages.Workspaces;
public partial class Workspace
{
    private bool _fight;
    private string AreaLink(string path) => AdminNavigation.InArea(path, _fight ? "fight" : "gym");
    private bool Can(string p) => AdminNavigation.CanRead(Session.Current?.User, p);
    protected override void OnParametersSet()
    {
        _fight = new Uri(Navigation.Uri).AbsolutePath == "/fight";
        Session.SelectArea(_fight ? "fight" : "gym");
    }
}
