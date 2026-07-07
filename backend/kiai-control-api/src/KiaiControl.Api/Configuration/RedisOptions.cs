namespace KiaiControl.Api.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "ConnectionStrings";

    public string Redis { get; init; } = string.Empty;
}
