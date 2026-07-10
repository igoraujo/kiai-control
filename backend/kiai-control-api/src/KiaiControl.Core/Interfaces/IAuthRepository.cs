using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAuthRepository
{
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AuthSession?> GetSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<AuthSession> CreateSessionAsync(AuthSession session, CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionByTokenHashAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionsByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    Task<bool> ReplaceSessionAsync(Guid currentSessionId, Guid newSessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordHashAsync(Guid userId, string passwordHash, DateTimeOffset updatedAt, CancellationToken cancellationToken = default);

    Task<AuthPasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<AuthPasswordResetToken> CreatePasswordResetTokenAsync(AuthPasswordResetToken token, CancellationToken cancellationToken = default);

    Task<bool> MarkPasswordResetTokenAsUsedAsync(Guid tokenId, DateTimeOffset usedAt, CancellationToken cancellationToken = default);

    Task<bool> RevokePasswordResetTokensByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);
}
