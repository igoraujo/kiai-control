namespace KiaiControl.Api.Configuration;

public sealed class PlatformOwnerOptions
{
    public const string SectionName = "PlatformOwner";

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
