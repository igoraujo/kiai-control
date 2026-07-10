using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.Services.Auth;

public sealed class JwtAuthTokenService(
    string issuer,
    string audience,
    string signingKey,
    int accessTokenExpirationMinutes,
    int refreshTokenExpirationDays) : IAuthTokenService
{
    public IssuedAccessToken CreateAccessToken(AuthUser user, DateTimeOffset now)
    {
        var expiresAt = now.AddMinutes(accessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("person_id", user.PersonId.ToString()),
            new("organization_id", user.OrganizationId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return new IssuedAccessToken
        {
            Token = tokenHandler.WriteToken(securityToken),
            ExpiresAt = expiresAt
        };
    }

    public string CreateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public DateTimeOffset GetRefreshTokenExpiration(DateTimeOffset now)
    {
        return now.AddDays(refreshTokenExpirationDays);
    }
}
