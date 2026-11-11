using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Platform;

namespace KiaiControl.Admin.Services;

public sealed partial class AdminApiClient
{
    public async Task ChangePlatformArchiveAsync(Guid id, bool archived, PlatformArchiveRequest request)
    {
        using var response = await SendAuthorizedAsync(archived ? HttpMethod.Delete : HttpMethod.Post,
            archived ? $"api/v1/platform/organizations/{id}" : $"api/v1/platform/organizations/{id}/restore", request);
    }

    public Task<PlatformAuditResponse> PlatformAuditDetailsAsync(Guid id, PlatformAuditDetailsRequest request) =>
        CreateAsync<PlatformAuditDetailsRequest, PlatformAuditResponse>($"platform/audit-logs/{id}/details", request);

    public Task<PlatformMetricsResponse> PlatformMetricsAsync(PlatformLogQuery query, int page) =>
        GetAsync<PlatformMetricsResponse>($"platform/metrics?{LogQuery(query, page)}");
    public Task<PlatformHealthResponse> PlatformHealthAsync() => GetAsync<PlatformHealthResponse>("platform/health");
    public Task<PlatformRetentionPolicyResponse> PlatformRetentionPolicyAsync() => GetAsync<PlatformRetentionPolicyResponse>("platform/retention-policy");
    public Task<PlatformOverviewResponse> PlatformOverviewAsync() => GetAsync<PlatformOverviewResponse>("platform/overview");
    public Task<PagedResponse<PlatformAccountResponse>> PlatformAccountsAsync(Guid organizationId, int page, string? search) =>
        GetAsync<PagedResponse<PlatformAccountResponse>>($"platform/organizations/{organizationId}/accounts?page={page}&pageSize=25&search={Uri.EscapeDataString(search ?? "")}");

    public async Task CreatePlatformAccountAsync(Guid organizationId, CreatePlatformAccountRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/platform/organizations/{organizationId}/accounts", request);
    }

    public async Task RevokePlatformSessionsAsync(Guid organizationId, Guid userId, PlatformReasonRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/platform/organizations/{organizationId}/accounts/{userId}/revoke-sessions", request);
    }

    public Task<PagedResponse<PlatformAuditResponse>> PlatformAuditAsync(PlatformLogQuery query, int page) =>
        GetAsync<PagedResponse<PlatformAuditResponse>>($"platform/audit-logs?{LogQuery(query, page)}&actor={Uri.EscapeDataString(query.Actor ?? "")}&action={Uri.EscapeDataString(query.Action ?? "")}");
    public Task<PagedResponse<PlatformIncidentResponse>> PlatformIncidentsAsync(PlatformLogQuery query, int page) =>
        GetAsync<PagedResponse<PlatformIncidentResponse>>($"platform/incidents?{LogQuery(query, page)}&category={Uri.EscapeDataString(query.Action ?? "")}");

    private static string LogQuery(PlatformLogQuery query, int page) =>
        $"page={page}&pageSize=25&organizationId={query.OrganizationId}&from={Uri.EscapeDataString(query.From.ToString("O"))}&to={Uri.EscapeDataString(query.To.ToString("O"))}";
}

public sealed record PlatformLogQuery(Guid? OrganizationId, DateTimeOffset From, DateTimeOffset To, string? Actor = null, string? Action = null)
{
    public static PlatformLogQuery Default => new(null, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
}
