using System.Security.Cryptography;
using System.Text.Json;
using KiaiControl.Contracts.Auth;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace KiaiControl.Admin.Services;

public sealed class ProtectedBrowserSessionStore(
    ProtectedLocalStorage storage,
    ILogger<ProtectedBrowserSessionStore> logger) : IAdminSessionStore
{
    private const string StorageKey = "kiai-control-admin-session";

    public async Task<SessionTokenResponse?> LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<SessionTokenResponse>(StorageKey);
            return result.Success ? result.Value : null;
        }
        catch (CryptographicException exception)
        {
            logger.LogWarning(exception, "Discarding an unreadable protected admin session.");
            await storage.DeleteAsync(StorageKey);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Discarding an invalid protected admin session.");
            await storage.DeleteAsync(StorageKey);
            return null;
        }
    }

    public async Task SaveAsync(SessionTokenResponse session)
    {
        await storage.SetAsync(StorageKey, session);
    }

    public async Task ClearAsync()
    {
        await storage.DeleteAsync(StorageKey);
    }
}
