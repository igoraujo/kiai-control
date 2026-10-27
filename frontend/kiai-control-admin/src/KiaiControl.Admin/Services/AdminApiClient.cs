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

    public async Task<IReadOnlyList<T>> ListAsync<T>(string resource, int page = 1, int pageSize = 100)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/{resource}?page={page}&pageSize={pageSize}");
        return await response.Content.ReadFromJsonAsync<List<T>>() ?? [];
    }

    public async Task<TResponse> CreateAsync<TRequest, TResponse>(string resource, TRequest body)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/{resource}", body);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<TResponse> UpdateAsync<TRequest, TResponse>(string resource, Guid id, TRequest body)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/{resource}/{id}", body);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task DeleteAsync(string resource, Guid id)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Delete, $"api/v1/{resource}/{id}");
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

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string requestUri, object? body = null, bool allowRefresh = true)
    {
        var currentSession = session.Current ?? throw new UnauthorizedAccessException();
        var client = httpClientFactory.CreateClient("KiaiApi");
        using var request = new HttpRequestMessage(method, requestUri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType());
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        request.Headers.Add("X-Organization-Id", currentSession.User.OrganizationId.ToString());

        var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized && allowRefresh)
        {
            response.Dispose();
            if (await RefreshSessionAsync())
            {
                return await SendAuthorizedAsync(method, requestUri, body, allowRefresh: false);
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

        if (!response.IsSuccessStatusCode)
        {
            var detail = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException(string.IsNullOrWhiteSpace(detail) ? $"Erro {(int)status} ao chamar a API." : detail, null, status);
        }

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
