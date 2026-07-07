namespace KiaiControl.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string PostgreSql { get; init; } = string.Empty;
}
