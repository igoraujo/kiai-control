using System.IdentityModel.Tokens.Jwt;
using KiaiControl.Core.Entities;
using KiaiControl.Services.Auth;

namespace KiaiControl.UnitTests;

public sealed class JwtAuthTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ShouldSerializeUserRolesAsRoleClaims()
    {
        var service = new JwtAuthTokenService(
            "kiai-test-issuer",
            "kiai-test-audience",
            "test-signing-key-that-is-long-enough-for-hmac-sha256",
            15,
            7);
        var token = service.CreateAccessToken(new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "teacher@example.test",
            IsActive = true,
            Roles = ["teacher", "admin"]
        }, DateTimeOffset.UtcNow);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);

        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "teacher");
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "admin");
    }
}