using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.Services;

public sealed class AdminSession
{
    public SessionTokenResponse? Current { get; private set; }

    public void Set(SessionTokenResponse session) => Current = session;

    public void Clear() => Current = null;

    public Task LogoutAsync()
    {
        Current = null;
        return Task.CompletedTask;
    }
}
