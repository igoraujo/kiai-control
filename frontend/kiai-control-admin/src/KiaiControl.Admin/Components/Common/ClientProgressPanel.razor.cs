namespace KiaiControl.Admin.Components.Common;
public partial class ClientProgressPanel
{
    [Parameter]
    public Guid? ClientId { get; set; }

    private ClientJourneyResponse? _journey;
    private string? _error;
    private bool _busy;
    private int _page = 1;
    protected override Task OnParametersSetAsync() => LoadAsync();
    private async Task ChangePageAsync(int page)
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
            _journey = await Api.ClientJourneyAsync(ClientId, _page);
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar a evolução. Tente novamente.";
        }
        finally
        {
            _busy = false;
        }
    }

    public static string AttendanceLabel(string status) => status switch
    {
        "present" => "Presente",
        "late" => "Atraso",
        "absent" => "Falta",
        "justified_absence" => "Falta justificada",
        "pending_confirmation" => "Aguardando confirmação",
        _ => status
    };
}
