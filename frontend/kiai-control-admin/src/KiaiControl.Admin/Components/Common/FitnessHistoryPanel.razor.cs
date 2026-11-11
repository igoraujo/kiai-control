namespace KiaiControl.Admin.Components.Common;
public partial class FitnessHistoryPanel
{
    [Parameter] public bool Personal {get;set;}=true;
    [Parameter]
    public Guid GoalId { get; set; }

    private IReadOnlyList<FitnessProgressResponse> _records = [];
    private int _page = 1;
    private string? _error;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task PageAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _error = null;
            _records = Personal ? await Api.PortalListAsync<FitnessProgressResponse>($"goals/{GoalId}/progress", _page) : await Api.ListFitnessProgressAsync(GoalId,_page,20);
        }
        catch (HttpRequestException)
        {
            _error = "Não foi possível carregar os registros.";
        }
    }
}
