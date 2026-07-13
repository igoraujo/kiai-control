namespace KiaiControl.Api.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public string Provider { get; init; } = "Memory";

    public string InstanceName { get; init; } = "kiai-control:";
}