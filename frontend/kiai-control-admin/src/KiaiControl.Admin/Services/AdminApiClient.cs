using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Reports;

namespace KiaiControl.Admin.Services;

public sealed class AdminApiClient(IHttpClientFactory httpClientFactory, AdminSession session)
{
    public async Task<SessionTokenResponse> LoginAsync(string email, string password)
    {
        var client = httpClientFactory.CreateClient("KiaiApi");
        using var response = await client.PostAsJsonAsync("api/v1/auth/login", new LoginRequest
        {
            Email = email.Trim(),
            Password = password
        });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionTokenResponse>()
            ?? throw new HttpRequestException("The API returned an empty session response.");
    }

    public async Task<DashboardReportResponse> GetDashboardAsync(DateOnly from, DateOnly to)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            $"api/v1/reports/dashboard?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return await response.Content.ReadFromJsonAsync<DashboardReportResponse>()
            ?? throw new HttpRequestException("The API returned an empty dashboard response.");
    }

    public async Task LogoutAsync()
    {
        var currentSession = session.Current;
        if (currentSession is null)
        {
            return;
        }

        var client = httpClientFactory.CreateClient("KiaiApi");
        using var response = await client.PostAsJsonAsync("api/v1/auth/logout", new LogoutRequest
        {
            RefreshToken = currentSession.RefreshToken
        });
        response.EnsureSuccessStatusCode();
        session.Clear();
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string requestUri, bool allowRefresh = true)
    {
        var currentSession = session.Current ?? throw new UnauthorizedAccessException();
        var client = httpClientFactory.CreateClient("KiaiApi");
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        request.Headers.Add("X-Organization-Id", currentSession.User.OrganizationId.ToString());

        var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized && allowRefresh)
        {
            response.Dispose();
            if (await RefreshSessionAsync())
            {
                return await SendAuthorizedAsync(method, requestUri, allowRefresh: false);
            }

            session.Clear();
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            session.Clear();
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<bool> RefreshSessionAsync()
    {
        var currentSession = session.Current;
        if (currentSession is null || currentSession.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var client = httpClientFactory.CreateClient("KiaiApi");
        using var response = await client.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = currentSession.RefreshToken
        });

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var refreshed = await response.Content.ReadFromJsonAsync<SessionTokenResponse>();
        if (refreshed is null)
        {
            return false;
        }

        session.Set(refreshed);
        return true;
    }
}
