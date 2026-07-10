using System.Security.Cryptography;
using System.Text;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Auth;

public sealed class AuthUseCase(
    IAuthRepository authRepository,
    IPasswordHasher passwordHasher,
    IAuthTokenService authTokenService)
{
    private const int PasswordResetExpirationMinutes = 30;

    public async Task<SessionTokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var user = await authRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var issued = await IssueSessionAsync(user, ipAddress, cancellationToken);
        return issued.Response;
    }

    public async Task<SessionTokenResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var now = DateTimeOffset.UtcNow;
        var refreshTokenHash = ComputeSha256(refreshToken);
        var currentSession = await authRepository.GetSessionByTokenHashAsync(refreshTokenHash, cancellationToken);

        if (currentSession is null || currentSession.RevokedAt.HasValue || currentSession.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await authRepository.GetByIdAsync(currentSession.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
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
            throw new UnauthorizedAccessException("Invalid user context.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("CurrentPassword and NewPassword are required.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new ArgumentException("NewPassword must have at least 8 characters.");
        }

        var user = await authRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid user context.");
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is invalid.");
        }

        var now = DateTimeOffset.UtcNow;
        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var updated = await authRepository.UpdatePasswordHashAsync(userId, newPasswordHash, now, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("Could not update password.");
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
            Email = user.Email
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        var normalizedEmail = request.Email.Trim();
        var user = await authRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse
            {
                Message = "If the e-mail exists, recovery instructions will be sent."
            };
        }

        var now = DateTimeOffset.UtcNow;
        var rawToken = authTokenService.CreateRefreshToken();
        var tokenHash = ComputeSha256(rawToken);
        var expiresAt = now.AddMinutes(PasswordResetExpirationMinutes);

        await authRepository.CreatePasswordResetTokenAsync(new AuthPasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress
        }, cancellationToken);

        return new ForgotPasswordResponse
        {
            Message = "If the e-mail exists, recovery instructions will be sent.",
            ResetToken = rawToken,
            ResetTokenExpiresAt = expiresAt
        };
    }

    public async Task<SimpleMessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Token and NewPassword are required.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new ArgumentException("NewPassword must have at least 8 characters.");
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = ComputeSha256(request.Token);
        var resetToken = await authRepository.GetPasswordResetTokenByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || resetToken.RevokedAt.HasValue || resetToken.UsedAt.HasValue || resetToken.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("Invalid reset token.");
        }

        var user = await authRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid reset token.");
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var updated = await authRepository.UpdatePasswordHashAsync(user.Id, newPasswordHash, now, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException("Could not update password.");
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
                Email = user.Email
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
