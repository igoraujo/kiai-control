namespace KiaiControl.Core.Entities;

public sealed class IssuedAccessToken
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }
}
