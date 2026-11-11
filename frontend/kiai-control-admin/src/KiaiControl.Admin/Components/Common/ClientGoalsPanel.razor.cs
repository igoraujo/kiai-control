namespace KiaiControl.Admin.Components.Common;
public partial class ClientGoalsPanel
{
    [Parameter] public Guid? ClientId {get;set;}
    private IReadOnlyList<FitnessGoalResponse> _goals = [];
    private int _page = 1;
    private bool _busy;
    private string? _error;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task PageAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            _goals = ClientId is null ? await Api.PortalListAsync<FitnessGoalResponse>("goals", _page) : await Api.ListFitnessGoalsAsync(_page,20,ClientId);
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar suas metas.";
        }
        finally
        {
            _busy = false;
        }
    }

    private static string Status(string s) => s switch
    {
        "active" => "Em andamento",
        "achieved" => "Concluída",
        "canceled" => "Cancelada",
        _ => "Encerrada"
    };
}
