using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Services.Email;

public sealed class DisabledEmailSender : IEmailSender
{
    public bool IsEnabled => false;
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new EmailSendResult(EmailSendStatus.Disabled));
    }
}
