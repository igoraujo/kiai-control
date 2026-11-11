namespace KiaiControl.Admin.Components.Pages;
public partial class ForgotPassword
{
    private string _email = "";
    private string? _message, _error;
    private bool _busy;
    private async Task SendAsync()
    {
        if (!System.Net.Mail.MailAddress.TryCreate(_email, out _))
        {
            _error = "Informe um e-mail válido.";
            return;
        }

        _busy = true;
        _error = null;
        try
        {
            _message = await Api.ForgotPasswordAsync(_email);
        }
        catch (HttpRequestException)
        {
            _error = "Não foi possível solicitar a recuperação. Aguarde um momento e tente novamente.";
        }
        finally
        {
            _busy = false;
        }
    }
}
