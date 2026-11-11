namespace KiaiControl.Api.Configuration;

public sealed class PlatformPrivacyOptions
{
    public int MetricsRetentionDays { get; set; } = 30;
    public int IncidentRetentionDays { get; set; } = 90;
    public int AuditIdentityRetentionDays { get; set; } = 180;
    public int FlushSeconds { get; set; } = 15;

    public bool IsValid() => MetricsRetentionDays is >= 1 and <= 3650
        && IncidentRetentionDays is >= 1 and <= 3650
        && AuditIdentityRetentionDays is >= 1 and <= 3650
        && FlushSeconds is >= 1 and <= 300;
}
