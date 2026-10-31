using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.Services;

public interface IAdminSessionStore
{
    Task<SessionTokenResponse?> LoadAsync();

    Task SaveAsync(SessionTokenResponse session);

    Task ClearAsync();
}
