using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IEmailSender
{
    bool IsEnabled { get; }
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
