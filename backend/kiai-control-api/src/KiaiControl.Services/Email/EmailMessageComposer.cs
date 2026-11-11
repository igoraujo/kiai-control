using KiaiControl.Core.Entities;
using MimeKit;

namespace KiaiControl.Services.Email;

internal static class EmailMessageComposer
{
    internal static bool IsAddressValid(string? value) => !string.IsNullOrWhiteSpace(value)
        && !HasNewline(value) && MailboxAddress.TryParse(value.Trim(), out var address)
        && address.Address == value.Trim() && address.Address.Contains('@');

    internal static bool HasNewline(string? value) => value?.IndexOfAny(['\r', '\n']) >= 0;

    public static MimeMessage Compose(EmailMessage message, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(message.Subject) || HasNewline(message.Subject))
            throw new ArgumentException("O assunto do e-mail é obrigatório e deve ocupar uma única linha.", nameof(message));
        if (string.IsNullOrWhiteSpace(message.TextBody) && string.IsNullOrWhiteSpace(message.HtmlBody))
            throw new ArgumentException("Informe o conteúdo em texto ou HTML do e-mail.", nameof(message));

        var mime = new MimeMessage();
        mime.From.Add(Mailbox(message.From ?? new(options.FromAddress, options.FromName), "From"));
        mime.To.Add(Mailbox(message.To, "To"));
        var replyTo = message.ReplyTo ?? (string.IsNullOrWhiteSpace(options.ReplyToAddress) ? null : new EmailAddress(options.ReplyToAddress, options.ReplyToName));
        if (replyTo is not null) mime.ReplyTo.Add(Mailbox(replyTo, "ReplyTo"));
        mime.Subject = message.Subject;
        var fromAddress = ((MailboxAddress)mime.From[0]).Address;
        mime.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId(fromAddress[(fromAddress.LastIndexOf('@') + 1)..]);
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();
        return mime;
    }

    private static MailboxAddress Mailbox(EmailAddress? value, string field)
    {
        if (value is null || !IsAddressValid(value.Address) || HasNewline(value.DisplayName))
            throw new ArgumentException($"O campo {field} deve conter um endereço válido e sem quebras de linha.");
        return new MailboxAddress(value.DisplayName ?? "", value.Address.Trim());
    }
}
