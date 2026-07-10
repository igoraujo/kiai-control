using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAuthTokenService
{
    IssuedAccessToken CreateAccessToken(AuthUser user, DateTimeOffset now);

    string CreateRefreshToken();

    DateTimeOffset GetRefreshTokenExpiration(DateTimeOffset now);
}
