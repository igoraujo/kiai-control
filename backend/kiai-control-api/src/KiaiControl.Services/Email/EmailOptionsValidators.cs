using Microsoft.Extensions.Options;

namespace KiaiControl.Services.Email;

internal sealed record EmailProviderRegistration(string Name);

internal sealed class EmailOptionsValidator(IEnumerable<EmailProviderRegistration> providers) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var errors = new List<string>();
        var provider = options.Provider?.Trim();
        if (!providers.Any(p => p.Name.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            errors.Add("Email:Provider não possui um adaptador registrado.");
        if (string.Equals(provider, "Disabled", StringComparison.OrdinalIgnoreCase))
            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);

        if (!EmailMessageComposer.IsAddressValid(options.FromAddress)) errors.Add("Configure um Email:FromAddress válido.");
        if (EmailMessageComposer.HasNewline(options.FromName) || EmailMessageComposer.HasNewline(options.ReplyToName))
            errors.Add("Os nomes de remetente/resposta não podem conter quebras de linha.");
        if (!string.IsNullOrWhiteSpace(options.ReplyToAddress) && !EmailMessageComposer.IsAddressValid(options.ReplyToAddress))
            errors.Add("Email:ReplyToAddress deve conter um endereço válido.");
        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            var smtp = options.Smtp;
            if (smtp is null) errors.Add("Configure a seção Email:Smtp.");
            else
            {
                if (string.IsNullOrWhiteSpace(smtp.Host) || Uri.CheckHostName(smtp.Host) == UriHostNameType.Unknown)
                    errors.Add("Email:Smtp:Host deve conter apenas o hostname ou IP do servidor.");
                if (smtp.Port is < 1 or > 65535) errors.Add("Email:Smtp:Port deve estar entre 1 e 65535.");
                if (!Enum.IsDefined(smtp.Security)) errors.Add("Email:Smtp:Security deve ser StartTls, SslOnConnect ou None.");
                if (smtp.TimeoutSeconds is < 1 or > 300) errors.Add("Email:Smtp:TimeoutSeconds deve estar entre 1 e 300.");
                if (string.IsNullOrWhiteSpace(smtp.Username) != string.IsNullOrEmpty(smtp.Password))
                    errors.Add("Configure usuário e senha SMTP juntos, ou deixe ambos vazios para relay sem autenticação.");
            }
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

internal sealed class PasswordRecoveryEmailOptionsValidator(bool allowInsecureLocalhost) : IValidateOptions<PasswordRecoveryEmailOptions>
{
    internal static bool IsUrlAllowed(string? value, bool allowLocalhost) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.IsNullOrEmpty(uri.UserInfo)
        && (uri.Scheme == Uri.UriSchemeHttps || (allowLocalhost && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

    public ValidateOptionsResult Validate(string? name, PasswordRecoveryEmailOptions options) =>
        string.IsNullOrWhiteSpace(options.ResetUrl) || IsUrlAllowed(options.ResetUrl, allowInsecureLocalhost)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("PasswordRecovery:ResetUrl deve usar HTTPS. Em Development, HTTP é permitido apenas em loopback.");
}
