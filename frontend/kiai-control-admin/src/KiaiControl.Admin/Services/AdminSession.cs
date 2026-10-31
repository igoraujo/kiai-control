using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.Services;

public sealed class AdminSession
{
    private readonly IAdminSessionStore? _store;

    public AdminSession(IAdminSessionStore? store = null)
    {
        _store = store;
    }

    public SessionTokenResponse? Current { get; private set; }

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Current = _store is null ? null : await _store.LoadAsync();
        IsInitialized = true;
    }

    public async Task SetAsync(SessionTokenResponse session)
    {
        if (_store is not null)
        {
            await _store.SaveAsync(session);
        }

        Current = session;
        IsInitialized = true;
    }

    public async Task ClearAsync()
    {
        Current = null;
        IsInitialized = true;
        if (_store is not null)
        {
            await _store.ClearAsync();
        }
    }

}
