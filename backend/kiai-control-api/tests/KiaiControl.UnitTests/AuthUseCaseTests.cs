using System.Security.Cryptography;
using System.Text;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Auth;

namespace KiaiControl.UnitTests;

public sealed class AuthUseCaseTests
{
    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsInvalid()
    {
        var repository = new FakeAuthRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenService = new FakeAuthTokenService();

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "admin@kiai.local",
            PasswordHash = "HASH:CorrectPass123",
            IsActive = true
        };

        repository.UsersByEmail[user.Email] = user;

        var useCase = new AuthUseCase(repository, passwordHasher, tokenService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "WrongPass"
        }, null, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowUnauthorized_WhenRefreshTokenIsExpired()
    {
        var repository = new FakeAuthRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenService = new FakeAuthTokenService();

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "admin@kiai.local",
            PasswordHash = "HASH:CorrectPass123",
            IsActive = true
        };

        repository.UsersById[user.Id] = user;

        var rawRefreshToken = "expired-refresh-token";
        var refreshHash = ComputeSha256(rawRefreshToken);
        repository.SessionsByTokenHash[refreshHash] = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var useCase = new AuthUseCase(repository, passwordHasher, tokenService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.RefreshAsync(rawRefreshToken, null, CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdatePasswordAndRevokeSessions()
    {
        var repository = new FakeAuthRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenService = new FakeAuthTokenService();

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "admin@kiai.local",
            PasswordHash = "HASH:OldPass123",
            IsActive = true
        };

        repository.UsersById[user.Id] = user;

        var useCase = new AuthUseCase(repository, passwordHasher, tokenService);

        await useCase.ChangePasswordAsync(user.Id, new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123",
            NewPassword = "NewPass456"
        }, CancellationToken.None);

        Assert.Equal("HASH:NewPass456", repository.LastUpdatedPasswordHash);
        Assert.Equal(user.Id, repository.LastRevokedSessionsUserId);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeSessionByTokenHash()
    {
        var repository = new FakeAuthRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenService = new FakeAuthTokenService();
        var useCase = new AuthUseCase(repository, passwordHasher, tokenService);

        var rawRefreshToken = "refresh-token-to-revoke";
        await useCase.LogoutAsync(rawRefreshToken, CancellationToken.None);

        Assert.Equal(ComputeSha256(rawRefreshToken), repository.LastRevokedSessionTokenHash);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public Dictionary<string, AuthUser> UsersByEmail { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<Guid, AuthUser> UsersById { get; } = [];

        public Dictionary<string, AuthSession> SessionsByTokenHash { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? LastRevokedSessionTokenHash { get; private set; }

        public Guid? LastRevokedSessionsUserId { get; private set; }

        public string? LastUpdatedPasswordHash { get; private set; }

        public Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            UsersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<AuthUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            UsersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<AuthSession?> GetSessionByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            SessionsByTokenHash.TryGetValue(tokenHash, out var session);
            return Task.FromResult(session);
        }

        public Task<AuthSession> CreateSessionAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            if (session.Id == Guid.Empty)
            {
                session.Id = Guid.NewGuid();
            }

            SessionsByTokenHash[session.TokenHash] = session;
            return Task.FromResult(session);
        }

        public Task<bool> RevokeSessionByTokenHashAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            LastRevokedSessionTokenHash = tokenHash;

            if (SessionsByTokenHash.TryGetValue(tokenHash, out var session))
            {
                session.RevokedAt = revokedAt;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> RevokeSessionsByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            LastRevokedSessionsUserId = userId;

            foreach (var session in SessionsByTokenHash.Values.Where(session => session.UserId == userId))
            {
                session.RevokedAt = revokedAt;
            }

            return Task.FromResult(true);
        }

        public Task<bool> ReplaceSessionAsync(Guid currentSessionId, Guid newSessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            var current = SessionsByTokenHash.Values.FirstOrDefault(session => session.Id == currentSessionId);
            if (current is null)
            {
                return Task.FromResult(false);
            }

            current.RevokedAt = revokedAt;
            current.ReplacedBySessionId = newSessionId;
            return Task.FromResult(true);
        }

        public Task<bool> UpdatePasswordHashAsync(Guid userId, string passwordHash, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
        {
            LastUpdatedPasswordHash = passwordHash;

            if (!UsersById.TryGetValue(userId, out var user))
            {
                return Task.FromResult(false);
            }

            user.PasswordHash = passwordHash;
            return Task.FromResult(true);
        }

        public Task<AuthPasswordResetToken?> GetPasswordResetTokenByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthPasswordResetToken?>(null);
        }

        public Task<AuthPasswordResetToken> CreatePasswordResetTokenAsync(AuthPasswordResetToken token, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(token);
        }

        public Task<bool> MarkPasswordResetTokenAsUsedAsync(Guid tokenId, DateTimeOffset usedAt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RevokePasswordResetTokensByUserIdAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string input)
        {
            return $"HASH:{input}";
        }

        public bool Verify(string input, string hash)
        {
            return hash == $"HASH:{input}";
        }
    }

    private sealed class FakeAuthTokenService : IAuthTokenService
    {
        public IssuedAccessToken CreateAccessToken(AuthUser user, DateTimeOffset now)
        {
            return new IssuedAccessToken
            {
                Token = "access-token",
                ExpiresAt = now.AddMinutes(15)
            };
        }

        public string CreateRefreshToken()
        {
            return "refresh-token";
        }

        public DateTimeOffset GetRefreshTokenExpiration(DateTimeOffset now)
        {
            return now.AddDays(7);
        }
    }
}
