using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Auth;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlAuthRepository(IDbConnectionFactory dbConnectionFactory) : IAuthRepository
{
    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(AuthQueries.GetUserByEmail, new { Email = email });
    }

    public async Task<AuthUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(AuthQueries.GetUserById, new { UserId = userId });
    }

    public async Task<AuthSession?> GetSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthSession>(AuthQueries.GetSessionByTokenHash, new { TokenHash = tokenHash });
    }

    public async Task<AuthSession> CreateSessionAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        session.Id = Guid.NewGuid();

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<AuthSession>(AuthQueries.InsertSession, session);
        return created;
    }

    public async Task<bool> RevokeSessionByTokenHashAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.RevokeSessionByTokenHash, new
        {
            TokenHash = tokenHash,
            RevokedAt = revokedAt
        });

        return affected > 0;
    }

    public async Task<bool> RevokeSessionsByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.RevokeSessionsByUserId, new
        {
            UserId = userId,
            RevokedAt = revokedAt
        });

        return affected > 0;
    }

    public async Task<bool> ReplaceSessionAsync(Guid currentSessionId, Guid newSessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.ReplaceSession, new
        {
            CurrentSessionId = currentSessionId,
            NewSessionId = newSessionId,
            RevokedAt = revokedAt
        });

        return affected > 0;
    }

    public async Task<bool> UpdatePasswordHashAsync(Guid userId, string passwordHash, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.UpdatePasswordHash, new
        {
            UserId = userId,
            PasswordHash = passwordHash,
            UpdatedAt = updatedAt
        });

        return affected > 0;
    }

    public async Task<AuthPasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthPasswordResetToken>(AuthQueries.GetPasswordResetTokenByTokenHash, new
        {
            TokenHash = tokenHash
        });
    }

    public async Task<AuthPasswordResetToken> CreatePasswordResetTokenAsync(AuthPasswordResetToken token, CancellationToken cancellationToken = default)
    {
        token.Id = Guid.NewGuid();

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<AuthPasswordResetToken>(AuthQueries.InsertPasswordResetToken, token);
    }

    public async Task<bool> MarkPasswordResetTokenAsUsedAsync(Guid tokenId, DateTimeOffset usedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.MarkPasswordResetTokenAsUsed, new
        {
            TokenId = tokenId,
            UsedAt = usedAt
        });

        return affected > 0;
    }

    public async Task<bool> RevokePasswordResetTokensByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AuthQueries.RevokePasswordResetTokensByUserId, new
        {
            UserId = userId,
            RevokedAt = revokedAt
        });

        return affected > 0;
    }
}
