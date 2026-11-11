namespace KiaiControl.Admin.Components.Pages;
public partial class Account
{
    private ClientPortalProfileResponse? _profile;
    protected override async Task OnInitializedAsync()
    {
        if (AdminNavigation.HasRole(Session.Current?.User, "client"))
        {
            try
            {
                _profile = await Api.GetAsync<ClientPortalProfileResponse>("portal/profile");
            }
            catch (HttpRequestException)
            {
                _error = "Seu cadastro de aluno não pôde ser carregado.";
            }
        }
    }

    private string _current = "", _new = "", _confirm = "";
    private bool _busy;
    private string? _error;
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_current))
        {
            _error = "Informe a senha atual.";
            return;
        }
        if (!PasswordPolicy.IsValid(_new))
        {
            _error = PasswordPolicy.Requirements;
            return;
        }
        if (_new != _confirm)
        {
            _error = "A confirmação da nova senha não corresponde.";
            return;
        }

        _busy = true;
        _error = null;
        try
        {
            await Api.ChangePasswordAsync(_current, _new);
            await Session.ClearAsync();
            Navigation.NavigateTo("/login");
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível alterar a senha. Confira a senha atual e tente novamente.";
        }
        finally
        {
            _busy = false;
        }
    }
}
