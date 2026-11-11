namespace KiaiControl.Core.Entities;

public sealed record EmailAddress(string Address, string? DisplayName = null);

public sealed class EmailMessage
{
    public required EmailAddress To { get; init; }
    public required string Subject { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public EmailAddress? From { get; init; }
    public EmailAddress? ReplyTo { get; init; }
}

public enum EmailSendStatus { Accepted, Disabled, Failed }
public enum EmailFailureKind { Network, Authentication, Security, Timeout, Rejected }

/// <summary>Accepted means accepted by the transport, not delivery to the recipient's inbox.</summary>
public sealed record EmailSendResult(EmailSendStatus Status, string? MessageId = null,
    EmailFailureKind? FailureKind = null, bool IsTransient = false);
