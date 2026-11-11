using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Services.Email;
using Microsoft.Extensions.Options;

namespace KiaiControl.Services.Auth;

public sealed class PasswordRecoveryEmailDelivery(IEmailSender sender, IOptions<PasswordRecoveryEmailOptions> options) : IPasswordRecoveryDelivery
{
    public bool IsConfigured => sender.IsEnabled && !string.IsNullOrWhiteSpace(options.Value.ResetUrl);

    public async Task SendAsync(string email, string token, CancellationToken ct)
    {
        if (!IsConfigured) return;
        var builder = new UriBuilder(options.Value.ResetUrl!) { Fragment = $"token={Uri.EscapeDataString(token)}" };
        await sender.SendAsync(new EmailMessage
        {
            To = new EmailAddress(email),
            Subject = "Recuperação de acesso · Kiai Control",
            TextBody = $"Use o link abaixo para cadastrar uma nova senha. Ele expira em 30 minutos.\n\n{builder.Uri.AbsoluteUri}\n\nSe você não pediu esta alteração, ignore esta mensagem."
        }, ct);
    }
}
