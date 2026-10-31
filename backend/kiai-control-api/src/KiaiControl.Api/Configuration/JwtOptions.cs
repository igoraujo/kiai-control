using System.Text;

namespace KiaiControl.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const string DevelopmentSigningKey = "kiai-control-development-signing-key-only-32-bytes-minimum";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 15;

    public int RefreshTokenExpirationDays { get; init; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        if (AccessTokenExpirationMinutes <= 0 || RefreshTokenExpirationDays <= 0)
        {
            throw new InvalidOperationException("JWT access and refresh token lifetimes must be positive.");
        }
    }

    public string ResolveSigningKey(bool isDevelopment)
    {
        var signingKey = SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("Jwt:SigningKey must be configured outside Development.");
            }

            signingKey = DevelopmentSigningKey;
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes for HS256.");
        }

        return signingKey;
    }
}
