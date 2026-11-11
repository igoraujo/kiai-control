using System.Net.Sockets;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiaiControl.Services.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public bool IsEnabled => true;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configured = options.Value;
        using var mime = EmailMessageComposer.Compose(message, configured);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configured.Smtp.TimeoutSeconds));
        using var client = new SmtpClient { Timeout = configured.Smtp.TimeoutSeconds * 1000 };
        var security = configured.Smtp.Security switch
        {
            SmtpConnectionSecurity.StartTls => SecureSocketOptions.StartTls,
            SmtpConnectionSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpConnectionSecurity.None => SecureSocketOptions.None,
            _ => throw new InvalidOperationException("Modo de segurança SMTP inválido.")
        };
        try
        {
            await client.ConnectAsync(configured.Smtp.Host, configured.Smtp.Port, security, timeout.Token);
            if (!string.IsNullOrWhiteSpace(configured.Smtp.Username))
                await client.AuthenticateAsync(configured.Smtp.Username, configured.Smtp.Password!, timeout.Token);
            await client.SendAsync(mime, timeout.Token);

            // SMTP has already accepted the message. A QUIT failure must not turn it into a retryable send.
            try { await client.DisconnectAsync(true, timeout.Token); }
            catch (Exception exception) when (exception is IOException or SocketException or SmtpProtocolException
                or SmtpCommandException or ServiceNotConnectedException or TimeoutException or OperationCanceledException)
            { logger.LogDebug("SMTP session cleanup failed after acceptance. MessageId: {MessageId}", mime.MessageId); }

            logger.LogInformation("Email accepted by transport. MessageId: {MessageId}", mime.MessageId);
            return new(EmailSendStatus.Accepted, mime.MessageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is IOException or SocketException or SmtpCommandException
            or SmtpProtocolException or AuthenticationException or SslHandshakeException or System.Security.Authentication.AuthenticationException
            or ServiceNotAuthenticatedException or ServiceNotConnectedException or NotSupportedException
            or OperationCanceledException or TimeoutException)
        {
            var (kind, transient) = exception switch
            {
                SslHandshakeException or System.Security.Authentication.AuthenticationException or NotSupportedException => (EmailFailureKind.Security, false),
                AuthenticationException or ServiceNotAuthenticatedException => (EmailFailureKind.Authentication, false),
                OperationCanceledException or TimeoutException => (EmailFailureKind.Timeout, true),
                SmtpCommandException command => (EmailFailureKind.Rejected, (int)command.StatusCode is >= 400 and < 500),
                _ => (EmailFailureKind.Network, true)
            };
            // Do not log exception text/protocol responses: providers may include recipients or credentials.
            logger.LogWarning("Email transport failed. Category: {Category}; MessageId: {MessageId}", kind, mime.MessageId);
            return new(EmailSendStatus.Failed, mime.MessageId, kind, transient);
        }
    }
}
