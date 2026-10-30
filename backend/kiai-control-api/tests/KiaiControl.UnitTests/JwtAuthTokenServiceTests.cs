using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KiaiControl.Core.Entities;
using KiaiControl.Services.Auth;
using Microsoft.IdentityModel.Tokens;

namespace KiaiControl.UnitTests;

public sealed class JwtAuthTokenServiceTests
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    [Fact]
    public void CreateAccessToken_ShouldSerializeUserRolesAsRoleClaims()
    {
        var service = new JwtAuthTokenService(
            "kiai-test-issuer",
            "kiai-test-audience",
            SigningKey,
            15,
            7);
        var token = service.CreateAccessToken(new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "teacher@example.test",
            IsActive = true,
            Roles = ["teacher", "admin"],
            Permissions = ["lessons.manage", "attendance.manage"]
        }, DateTimeOffset.UtcNow);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "teacher");
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "admin");
        Assert.Contains(jwt.Claims, claim => claim.Type == "permission" && claim.Value == "lessons.manage");
        Assert.Contains(jwt.Claims, claim => claim.Type == "permission" && claim.Value == "attendance.manage");
    }

    [Fact]
    public void CreateAccessToken_ShouldValidateIssuerAudienceSignatureAndRoleClaims()
    {
        var service = new JwtAuthTokenService("kiai-test-issuer", "kiai-test-audience", SigningKey, 15, 7);
        var token = service.CreateAccessToken(new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "teacher@example.test",
            IsActive = true,
            Roles = ["teacher"],
            Permissions = ["lessons.manage"]
        }, DateTimeOffset.UtcNow);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "kiai-test-issuer",
            ValidateAudience = true,
            ValidAudience = "kiai-test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = tokenHandler.ValidateToken(token.Token, validationParameters, out _);

        Assert.True(principal.IsInRole("teacher"));
        Assert.Contains(principal.Claims, claim => claim.Type == "permission" && claim.Value == "lessons.manage");
    }
}