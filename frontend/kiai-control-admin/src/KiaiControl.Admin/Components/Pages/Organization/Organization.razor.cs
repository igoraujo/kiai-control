namespace KiaiControl.Admin.Components.Pages.Organization;
public partial class Organization
{
    private UpdateOrganizationRequest _form = new();
    private OrganizationSettingsResponse _settings = new();
    private bool _loading = true, _saving;
    private string? _error;
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var org = await Api.OrganizationAsync(Session.Current!.User.OrganizationId);
            _form = new()
            {
                Name = org.Name,
                TradeName = org.TradeName,
                Document = org.Document,
                Email = org.Email,
                Phone = org.Phone
            };
            _settings = await Api.OrganizationSettingsAsync();
        }
        catch (HttpRequestException)
        {
            _error = "Não foi possível carregar a academia. Atualize a página para tentar novamente.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveAsync()
    {
        _saving = true;
        _error = null;
        try
        {
            await Api.SaveOrganizationSettingsAsync(_settings);
            var org = await Api.UpdateAsync<UpdateOrganizationRequest, OrganizationResponse>("organizations", Session.Current!.User.OrganizationId, _form);
            Session.OrganizationName = org.Name;
            Session.SelectArea(Session.Area);
            Snackbar.Add("Configurações salvas.", Severity.Success);
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = e.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
