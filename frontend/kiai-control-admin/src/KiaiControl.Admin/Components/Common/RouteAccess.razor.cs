namespace KiaiControl.Admin.Components.Common;
public partial class RouteAccess
{
    [Parameter, EditorRequired]
    public Microsoft.AspNetCore.Components.RouteData RouteData { get; set; } = default!;

    private bool _allowed;
    protected override void OnInitialized() => Session.WorkspaceChanged += Changed;
    private void Changed() => _ = InvokeAsync(() =>
    {
        OnParametersSet();
        StateHasChanged();
    });
    public void Dispose() => Session.WorkspaceChanged -= Changed;
    protected override void OnParametersSet()
    {
        var path = new Uri(Navigation.Uri).AbsolutePath;
        _allowed = AdminNavigation.CanVisit(Session.Current?.User, path,Session.Capabilities);
        if (!_allowed && (Session.Current is null || Session.Current.User.MustChangePassword || path == "/"))
            Navigation.NavigateTo(AdminNavigation.Home(Session.Current?.User), replace: true);
    }
}
