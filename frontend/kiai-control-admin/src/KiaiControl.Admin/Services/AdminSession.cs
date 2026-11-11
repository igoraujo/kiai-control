using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.Services;
public sealed class AdminSession
{
    private readonly IAdminSessionStore? _store;
    public AdminSession(IAdminSessionStore? store = null)
    {
        _store = store;
    }

    public string Area { get; private set; } = "shared";
    public string OrganizationName { get; set; } = "Minha academia";

    public event Action? WorkspaceChanged;
    public void SetOrganizationName(string name)
    {
        OrganizationName = name;
        WorkspaceChanged?.Invoke();
    }

    public KiaiControl.Contracts.Modalities.WorkspaceCapabilitiesResponse? Capabilities { get; private set; }

    public void SetCapabilities(KiaiControl.Contracts.Modalities.WorkspaceCapabilitiesResponse value)
    {
        Capabilities = value;
        if (!string.IsNullOrWhiteSpace(value.OrganizationName))
            OrganizationName = value.OrganizationName;
        WorkspaceChanged?.Invoke();
    }

    public void SelectArea(string area)
    {
        var next = area is "gym" or "fight" ? area : "shared";
        if (Area == next)
            return;
        Area = next;
        WorkspaceChanged?.Invoke();
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
        WorkspaceChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        Current = null;
        Area = "shared";
        Capabilities = null;
        OrganizationName = "Minha academia";
        IsInitialized = true;
        if (_store is not null)
        {
            await _store.ClearAsync();
        }

        WorkspaceChanged?.Invoke();
    }
}
