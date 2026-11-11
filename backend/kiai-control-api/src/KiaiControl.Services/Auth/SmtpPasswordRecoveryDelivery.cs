using System.Net;
using System.Net.Mail;
using KiaiControl.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KiaiControl.Services.Auth;
public sealed class SmtpPasswordRecoveryDelivery(string? host, int port, string? username, string? password, string? from, string? resetUrl, ILogger<SmtpPasswordRecoveryDelivery> logger) : IPasswordRecoveryDelivery
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(host) && MailAddress.TryCreate(from, out _) && port is> 0 and <= 65535 && Uri.TryCreate(resetUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    public async Task SendAsync(string email, string token, CancellationToken ct)
    {
        if (!IsConfigured)
            return;
        using var client = new SmtpClient(host!, port)
        {
            EnableSsl = true
        };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);
        var url = $"{resetUrl!.Split('#')[0]}#token={Uri.EscapeDataString(token)}";
        using var message = new MailMessage(from!, email, "Recuperação de acesso · Kiai Control", $"Use o link abaixo para cadastrar uma nova senha. Ele expira em 30 minutos.\n\n{url}\n\nSe você não pediu esta alteração, ignore esta mensagem.");
        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (SmtpException)
        {
            logger.LogWarning("Falha na entrega SMTP de recuperação de senha. Verifique o provedor configurado.");
        }
    }
}
