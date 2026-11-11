namespace KiaiControl.Admin.Components.Common;
public partial class PageControls
{
    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public int? Total { get; set; }

    [Parameter]
    public bool HasNext { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public EventCallback<int> Changed { get; set; }
}
