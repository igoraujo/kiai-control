namespace KiaiControl.Admin.Components.Common;
public partial class AcademyName
{
    protected override void OnInitialized() => Session.WorkspaceChanged += Changed;
    private void Changed() => _ = InvokeAsync(StateHasChanged);
    public void Dispose() => Session.WorkspaceChanged -= Changed;
}
