namespace KiaiControl.Services.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Disabled";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Kiai Control";
    public string? ReplyToAddress { get; set; }
    public string? ReplyToName { get; set; }
    public SmtpEmailOptions Smtp { get; set; } = new();
}

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public SmtpConnectionSecurity Security { get; set; } = SmtpConnectionSecurity.StartTls;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public enum SmtpConnectionSecurity { StartTls, SslOnConnect, None }

public sealed class PasswordRecoveryEmailOptions
{
    public const string SectionName = "PasswordRecovery";
    public string? ResetUrl { get; set; }
}
