using System.Security.Cryptography;
using System.Text;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Auth;
public sealed class AuthUseCase(IAuthRepository authRepository, IPasswordHasher passwordHasher, IAuthTokenService authTokenService, IPasswordRecoveryDelivery? recoveryDelivery = null)
{
    private const int PasswordResetExpirationMinutes = 30;
    public async Task<SessionTokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(SystemMessages.EmailAndPasswordAreRequired);
        }

        var user = await authRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidCredentials);
        }

        var issued = await IssueSessionAsync(user, ipAddress, cancellationToken);
        return issued.Response;
    }

    public async Task<SessionTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidRefreshToken);
        }

        var now = DateTimeOffset.UtcNow;
        var refreshTokenHash = ComputeSha256(refreshToken);
        var currentSession = await authRepository.GetSessionByTokenHashAsync(refreshTokenHash, cancellationToken);
        if (currentSession is null || currentSession.RevokedAt.HasValue || currentSession.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidRefreshToken);
        }

        var user = await authRepository.GetByIdAsync(currentSession.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidRefreshToken);
        }

        var refreshedSession = await IssueSessionAsync(user, ipAddress, cancellationToken);
        await authRepository.ReplaceSessionAsync(currentSession.Id, refreshedSession.SessionId, now, cancellationToken);
        return refreshedSession.Response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = ComputeSha256(refreshToken);
        _ = await authRepository.RevokeSessionByTokenHashAsync(tokenHash, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidUserContext);
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException(SystemMessages.CurrentPasswordAndNewPasswordAreRequired);
        }

        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            throw new ArgumentException(PasswordPolicy.Requirements);
        }

        var user = await authRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidUserContext);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(SystemMessages.CurrentPasswordIsInvalid);
        }

        var now = DateTimeOffset.UtcNow;
        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var updated = await authRepository.UpdatePasswordHashAsync(userId, newPasswordHash, now, cancellationToken);
        if (!updated)
        {
            throw new InvalidOperationException(SystemMessages.CouldNotUpdatePassword);
        }

        _ = await authRepository.RevokeSessionsByUserIdAsync(userId, now, cancellationToken);
        _ = await authRepository.RevokePasswordResetTokensByUserIdAsync(userId, now, cancellationToken);
    }

    public async Task<AuthUserProfileResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await authRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return new AuthUserProfileResponse
        {
            UserId = user.Id,
            PersonId = user.PersonId,
            OrganizationId = user.OrganizationId,
            Email = user.Email,
            Roles = user.Roles,
            Permissions = user.Permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(SystemMessages.EmailIsRequired);
        }

        var normalizedEmail = request.Email.Trim();
        var user = await authRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse
            {
                Message = "Se o e-mail estiver cadastrado e a entrega estiver habilitada, você receberá as instruções. Você também pode solicitar uma nova senha à academia."
            };
        }

        var now = DateTimeOffset.UtcNow;
        var rawToken = authTokenService.CreateRefreshToken();
        var tokenHash = ComputeSha256(rawToken);
        var expiresAt = now.AddMinutes(PasswordResetExpirationMinutes);
        await authRepository.CreatePasswordResetTokenAsync(new AuthPasswordResetToken { UserId = user.Id, TokenHash = tokenHash, ExpiresAt = expiresAt, CreatedAt = now, CreatedByIp = ipAddress }, cancellationToken);
        if (recoveryDelivery?.IsConfigured == true)
            await recoveryDelivery.SendAsync(user.Email, rawToken, cancellationToken);
        return new ForgotPasswordResponse
        {
            Message = "Se o e-mail estiver cadastrado e a entrega estiver habilitada, você receberá as instruções. Você também pode solicitar uma nova senha à academia."
        };
    }

    public async Task<SimpleMessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException(SystemMessages.TokenAndNewPasswordAreRequired);
        }

        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            throw new ArgumentException(PasswordPolicy.Requirements);
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = ComputeSha256(request.Token);
        var resetToken = await authRepository.GetPasswordResetTokenByTokenHashAsync(tokenHash, cancellationToken);
        if (resetToken is null || resetToken.RevokedAt.HasValue || resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidResetToken);
        }

        var user = await authRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(SystemMessages.InvalidResetToken);
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var updated = await authRepository.UpdatePasswordHashAsync(user.Id, newPasswordHash, now, cancellationToken);
        if (!updated)
        {
            throw new InvalidOperationException(SystemMessages.CouldNotUpdatePassword);
        }

        _ = await authRepository.MarkPasswordResetTokenAsUsedAsync(resetToken.Id, now, cancellationToken);
        _ = await authRepository.RevokePasswordResetTokensByUserIdAsync(user.Id, now, cancellationToken);
        _ = await authRepository.RevokeSessionsByUserIdAsync(user.Id, now, cancellationToken);
        return new SimpleMessageResponse
        {
            Message = "Password changed successfully."
        };
    }

    private async Task<(SessionTokenResponse Response, Guid SessionId)> IssueSessionAsync(AuthUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var accessToken = authTokenService.CreateAccessToken(user, now);
        var refreshToken = authTokenService.CreateRefreshToken();
        var refreshTokenHash = ComputeSha256(refreshToken);
        var refreshTokenExpiresAt = authTokenService.GetRefreshTokenExpiration(now);
        var session = new AuthSession
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress
        };
        var createdSession = await authRepository.CreateSessionAsync(session, cancellationToken);
        var response = new SessionTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = createdSession.ExpiresAt,
            User = new AuthUserProfileResponse
            {
                UserId = user.Id,
                PersonId = user.PersonId,
                OrganizationId = user.OrganizationId,
                Email = user.Email,
                Roles = user.Roles,
                Permissions = user.Permissions,
                MustChangePassword = user.MustChangePassword
            }
        };
        return (response, createdSession.Id);
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
