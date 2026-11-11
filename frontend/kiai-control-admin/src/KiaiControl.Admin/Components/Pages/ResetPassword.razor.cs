namespace KiaiControl.Admin.Components.Pages;
public partial class ResetPassword
{
    private string _token = "", _password = "", _confirm = "";
    private string? _error;
    private bool _busy;
    protected override void OnInitialized()
    {
        var fragment = new Uri(Navigation.Uri).Fragment.TrimStart('#');
        if (fragment.StartsWith("token=", StringComparison.Ordinal))
            _token = Uri.UnescapeDataString(fragment[6..]);
    }

    private async Task SaveAsync()
    {
        if (!PasswordPolicy.IsValid(_password))
        {
            _error = PasswordPolicy.Requirements;
            return;
        }
        if (_password != _confirm)
        {
            _error = "A confirmação da nova senha não corresponde.";
            return;
        }

        _busy = true;
        try
        {
            await Api.ResetPasswordAsync(_token, _password);
            _token = "";
            Navigation.NavigateTo("/login", replace: true);
        }
        catch (HttpRequestException)
        {
            _error = "O link expirou ou não pôde ser validado. Solicite uma nova recuperação.";
        }
        finally
        {
            _busy = false;
        }
    }
}
