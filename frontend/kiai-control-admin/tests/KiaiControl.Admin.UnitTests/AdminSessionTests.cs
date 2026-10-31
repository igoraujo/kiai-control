using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.UnitTests;

public sealed class AdminSessionTests
{
    [Fact]
    public async Task InitializeAsync_ShouldRestorePersistedSession()
    {
        var storedSession = CreateSession();
        var store = new InMemoryAdminSessionStore { Session = storedSession };
        var session = new AdminSession(store);

        await session.InitializeAsync();

        Assert.True(session.IsInitialized);
        Assert.Same(storedSession, session.Current);
    }

    [Fact]
    public async Task SetAsync_ShouldPersistUpdatedSession()
    {
        var store = new InMemoryAdminSessionStore();
        var session = new AdminSession(store);
        var expected = CreateSession();

        await session.SetAsync(expected);

        Assert.Same(expected, session.Current);
        Assert.Same(expected, store.Session);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemovePersistedSession()
    {
        var store = new InMemoryAdminSessionStore { Session = CreateSession() };
        var session = new AdminSession(store);
        await session.InitializeAsync();

        await session.ClearAsync();

        Assert.Null(session.Current);
        Assert.Null(store.Session);
    }

    private static SessionTokenResponse CreateSession() => new()
    {
        AccessToken = "access-token",
        AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
        RefreshToken = "refresh-token",
        RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        User = new AuthUserProfileResponse
        {
            UserId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "admin@example.test",
            Roles = ["owner"]
        }
    };

    private sealed class InMemoryAdminSessionStore : IAdminSessionStore
    {
        public SessionTokenResponse? Session { get; set; }

        public Task<SessionTokenResponse?> LoadAsync() => Task.FromResult(Session);

        public Task SaveAsync(SessionTokenResponse session)
        {
            Session = session;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Session = null;
            return Task.CompletedTask;
        }
    }
}
