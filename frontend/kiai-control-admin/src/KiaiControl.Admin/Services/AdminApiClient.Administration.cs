using System.Net.Http.Json;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Administration;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Clients;
using KiaiControl.Contracts.Organizations;

namespace KiaiControl.Admin.Services;
public sealed partial class AdminApiClient
{
    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response)
    {
        var body = (await response.Content.ReadAsStringAsync()).Trim();
        if (body.Length == 0)
            return $"Não foi possível concluir a operação (HTTP {(int)response.StatusCode}).";
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind == System.Text.Json.JsonValueKind.String)
                return root.GetString() ?? "Falha na operação.";
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == System.Text.Json.JsonValueKind.Object)
                    return string.Join(" ", errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray()).Select(v => v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v)).Take(3));
                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == System.Text.Json.JsonValueKind.String)
                    return detail.GetString()!;
                if (root.TryGetProperty("title", out var title) && title.ValueKind == System.Text.Json.JsonValueKind.String)
                    return title.GetString()!;
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return body.Length > 500 ? "Não foi possível concluir a operação. Tente novamente." : body;
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        using var r = await httpClientFactory.CreateClient("KiaiApi").PostAsJsonAsync("api/v1/auth/forgot-password", new ForgotPasswordRequest { Email = email.Trim() });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ForgotPasswordResponse>())?.Message ?? "Solicitação registrada.";
    }

    public async Task ResetPasswordAsync(string token, string password)
    {
        using var r = await httpClientFactory.CreateClient("KiaiApi").PostAsJsonAsync("api/v1/auth/reset-password", new ResetPasswordRequest { Token = token, NewPassword = password });
        r.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<KiaiControl.Contracts.Branches.BranchResponse>> BranchOptionsAsync(string resource)
    {
        var items = new List<KiaiControl.Contracts.Branches.BranchResponse>();
        for (var page = 1;; page++)
        {
            var r = await CatalogAsync<KiaiControl.Contracts.Branches.BranchResponse>(resource + "/branches", page, 200);
            items.AddRange(r.Items);
            if (items.Count >= r.Total)
                return items;
        }
    }

    public async Task<IReadOnlyList<T>> AllAsync<T>(string resource)
    {
        var items = new List<T>();
        for (var page = 1;; page++)
        {
            var batch = await ListAsync<T>(resource, page, 200);
            items.AddRange(batch);
            if (batch.Count < 200)
                return items;
        }
    }

    public Task<IReadOnlyList<KiaiControl.Contracts.Billing.BillingResponse>> InvoicePageAsync(int page, int size, string? search) => GetListAsync<KiaiControl.Contracts.Billing.BillingResponse>($"api/v1/billing?page={page}&pageSize={size}&search={Uri.EscapeDataString(search ?? "")}");
    public static async Task<IReadOnlyList<T>> AllPagesAsync<T>(Func<int, Task<IReadOnlyList<T>>> read)
    {
        var result = new List<T>();
        for (var page = 1;; page++)
        {
            var batch = await read(page);
            result.AddRange(batch);
            if (batch.Count < 200)
                return result;
        }
    }

    public Task<PagedResponse<T>> CatalogAsync<T>(string resource, int page, int size, string? search = null, string? area = null) => GetAsync<PagedResponse<T>>($"catalog/{resource}?page={page}&pageSize={size}&search={Uri.EscapeDataString(search ?? "")}&area={AcademyArea.Normalize(area)}");
    public Task<PagedResponse<DirectoryPersonResponse>> PeopleAsync(int page, int size, string? search = null) => GetAsync<PagedResponse<DirectoryPersonResponse>>($"people?page={page}&pageSize={size}&search={Uri.EscapeDataString(search ?? "")}");
    public async Task SavePersonAsync(Guid? id, SavePersonRequest request)
    {
        using var response = await SendAuthorizedAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/v1/people" : $"api/v1/people/{id}", request);
    }

    public async Task AddProfileAsync(Guid id, string profile)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/people/{id}/profiles/{profile}");
    }

    public async Task SetAccountEmailAsync(Guid id, string email)
    {
        using var r = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/people/{id}/account/email", new AccountEmailRequest { Email = email });
    }

    public async Task SetAccountStateAsync(Guid id, bool active)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/people/{id}/account/state", new AccountStateRequest { IsActive = active });
    }

    public async Task ResetAccountPasswordAsync(Guid id, string password)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/people/{id}/account/password", new TemporaryPasswordRequest { Password = password });
    }

    public async Task RevokeAccountSessionsAsync(Guid id)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/people/{id}/account/revoke-sessions");
    }

    public async Task AssignPersonRoleAsync(Guid id, string role)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/access/people/{id}/roles", new AssignRoleRequest { RoleCode = role });
    }

    public async Task RevokePersonRoleAsync(Guid id, string role)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Delete, $"api/v1/access/people/{id}/roles/{Uri.EscapeDataString(role)}");
    }

    public Task<OrganizationResponse> OrganizationAsync(Guid id) => GetAsync<OrganizationResponse>($"organizations/{id}");
    public Task<OrganizationSettingsResponse> OrganizationSettingsAsync() => GetAsync<OrganizationSettingsResponse>("organizations/settings");
    public Task SaveOrganizationSettingsAsync(OrganizationSettingsResponse request) => PutAsync("organizations/settings", request);
    public Task<ClientJourneyResponse> ClientJourneyAsync(Guid? id, int page = 1, int size = 20) => GetAsync<ClientJourneyResponse>(id is null ? $"portal/journey?page={page}&pageSize={size}" : $"clients/{id}/progress?page={page}&pageSize={size}");
    public Task<IReadOnlyList<T>> PortalListAsync<T>(string resource, int page = 1, int size = 20) => GetListAsync<T>($"api/v1/portal/{resource}?page={page}&pageSize={size}");
}
