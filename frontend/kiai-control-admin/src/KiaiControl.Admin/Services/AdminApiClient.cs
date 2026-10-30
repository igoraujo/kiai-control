using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Attendance;
using KiaiControl.Contracts.Billing;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Modalities;
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

    public Task<IReadOnlyList<ModalityResponse>> ListAssignableClientModalitiesAsync() =>
        ListAsync<ModalityResponse>("clients/modalities", page: 1, pageSize: 200);

    public Task<IReadOnlyList<ModalityResponse>> ListClientModalitiesAsync(Guid clientId) =>
        ListAsync<ModalityResponse>($"clients/{clientId}/modalities", page: 1, pageSize: 200);

    public async Task LinkClientModalityAsync(Guid clientId, Guid modalityId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/clients/{clientId}/modalities/{modalityId}");
    }

    public Task UnlinkClientModalityAsync(Guid clientId, Guid modalityId) =>
        DeleteAsync($"clients/{clientId}/modalities", modalityId);

    public Task<IReadOnlyList<BillingPlanResponse>> ListBillingPlansAsync(int page = 1, int pageSize = 100) =>
        ListAsync<BillingPlanResponse>("billing/plans", page, pageSize);

    public Task<BillingPlanResponse> CreateBillingPlanAsync(CreateBillingPlanRequest request) =>
        CreateAsync<CreateBillingPlanRequest, BillingPlanResponse>("billing/plans", request);

    public Task<BillingPlanResponse> UpdateBillingPlanAsync(Guid planId, UpdateBillingPlanRequest request) =>
        UpdateAsync<UpdateBillingPlanRequest, BillingPlanResponse>("billing/plans", planId, request);

    public Task DeactivateBillingPlanAsync(Guid planId) => DeleteAsync("billing/plans", planId);

    public Task<IReadOnlyList<ClientPlanSubscriptionResponse>> ListClientSubscriptionsAsync(int page = 1, int pageSize = 100) =>
        ListAsync<ClientPlanSubscriptionResponse>("billing/subscriptions", page, pageSize);

    public Task<ClientPlanSubscriptionResponse> CreateClientSubscriptionAsync(Guid clientId, CreateClientSubscriptionRequest request) =>
        CreateAsync<CreateClientSubscriptionRequest, ClientPlanSubscriptionResponse>($"billing/clients/{clientId}/subscriptions", request);

    public async Task<ClientPlanSubscriptionResponse> CancelClientSubscriptionAsync(
        Guid subscriptionId,
        CancelClientSubscriptionRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/billing/subscriptions/{subscriptionId}/cancel", request);
        return await response.Content.ReadFromJsonAsync<ClientPlanSubscriptionResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public Task<IReadOnlyList<BillingResponse>> ListInvoicesAsync(int page = 1, int pageSize = 100) =>
        ListAsync<BillingResponse>("billing", page, pageSize);

    public async Task<IReadOnlyList<BillingResponse>> GenerateInvoicesAsync(GenerateInvoicesRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, "api/v1/billing/invoices/generate", request);
        return await response.Content.ReadFromJsonAsync<List<BillingResponse>>() ?? [];
    }

    public async Task<PaymentResponse> RegisterInvoicePaymentAsync(Guid invoiceId, RegisterPaymentRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/billing/{invoiceId}/payments", request);
        return await response.Content.ReadFromJsonAsync<PaymentResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(int page = 1, int pageSize = 100) =>
        ListAsync<PaymentResponse>("billing/payments", page, pageSize);

    public async Task<IReadOnlyList<RoleResponse>> ListAccessRolesAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/access/roles");
        return await response.Content.ReadFromJsonAsync<List<RoleResponse>>() ?? [];
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListAccessPermissionsAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/access/permissions");
        return await response.Content.ReadFromJsonAsync<List<PermissionResponse>>() ?? [];
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListRolePermissionsAsync(string roleCode)
    {
        var escapedRoleCode = Uri.EscapeDataString(roleCode);
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/access/roles/{escapedRoleCode}/permissions");
        return await response.Content.ReadFromJsonAsync<List<PermissionResponse>>() ?? [];
    }

    public async Task SetRolePermissionAsync(string roleCode, string permissionCode, bool isGranted)
    {
        var escapedRoleCode = Uri.EscapeDataString(roleCode);
        var escapedPermissionCode = Uri.EscapeDataString(permissionCode);
        if (isGranted)
        {
            await PutAsync($"access/roles/{escapedRoleCode}/permissions", new SetRolePermissionRequest
            {
                PermissionCode = permissionCode
            });
            return;
        }

        await DeleteAsync($"access/roles/{escapedRoleCode}/permissions/{escapedPermissionCode}", null);
    }

    public Task<IReadOnlyList<LessonResponse>> ListLessonsAsync(int page = 1, int pageSize = 200) =>
        ListAsync<LessonResponse>("lessons", page, pageSize);

    public Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request) =>
        CreateAsync<CreateLessonRequest, LessonResponse>("lessons", request);

    public async Task<IReadOnlyList<LessonResponse>> GenerateLessonsAsync(GenerateLessonsRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, "api/v1/lessons/generate", request);
        return await response.Content.ReadFromJsonAsync<List<LessonResponse>>() ?? [];
    }

    public async Task<LessonResponse> CancelLessonAsync(Guid lessonId, CancelLessonRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/lessons/{lessonId}/cancel", request);
        return await response.Content.ReadFromJsonAsync<LessonResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<LessonResponse> RescheduleLessonAsync(Guid lessonId, RescheduleLessonRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/lessons/{lessonId}/reschedule", request);
        return await response.Content.ReadFromJsonAsync<LessonResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> GetLessonAttendanceAsync(Guid lessonId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/lessons/{lessonId}/attendances");
        return await response.Content.ReadFromJsonAsync<List<AttendanceRecordResponse>>() ?? [];
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> SaveLessonAttendanceAsync(
        Guid lessonId,
        SaveAttendanceCallRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/lessons/{lessonId}/attendances", request);
        return await response.Content.ReadFromJsonAsync<List<AttendanceRecordResponse>>() ?? [];
    }

    public Task<IReadOnlyList<AttendanceRecordResponse>> ListPendingCheckInsAsync(int page = 1, int pageSize = 100) =>
        ListAsync<AttendanceRecordResponse>("attendance/pending", page, pageSize);

    public async Task<AttendanceRecordResponse> ValidateCheckInAsync(Guid attendanceId, string status)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/attendance/{attendanceId}/validate", new ValidateCheckInRequest { Status = status });
        return await response.Content.ReadFromJsonAsync<AttendanceRecordResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<AttendanceRecordResponse> CheckInAsync(Guid lessonId)
    {
        var client = httpClientFactory.CreateClient("KiaiApi");
        var currentSession = session.Current ?? throw new UnauthorizedAccessException();
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/attendance/check-in")
        {
            Content = JsonContent.Create(new CheckInRequest { LessonId = lessonId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentSession.AccessToken);
        request.Headers.Add("X-Organization-Id", currentSession.User.OrganizationId.ToString());
        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AttendanceRecordResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string resource, int page = 1, int pageSize = 100)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/{resource}?page={page}&pageSize={pageSize}");
        return await response.Content.ReadFromJsonAsync<List<T>>() ?? [];
    }

    public async Task<T> GetAsync<T>(string resource)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/{resource}");
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
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

    public async Task PutAsync<TRequest>(string resource, TRequest body)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Put, $"api/v1/{resource}", body);
    }

    public async Task PatchAsync<TRequest>(string resource, TRequest body)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/{resource}", body);
    }

    public async Task DeleteAsync(string resource, Guid id)
    {
        await DeleteAsync(resource, id, null);
    }

    public async Task DeleteAsync(string resource, Guid id, string? queryString)
    {
        var uri = $"api/v1/{resource}/{id}";
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            uri += queryString.StartsWith('?') ? queryString : $"?{queryString}";
        }

        using var response = await SendAuthorizedAsync(HttpMethod.Delete, uri);
    }

    public async Task DeleteAsync(string resource, string? queryString)
    {
        var uri = $"api/v1/{resource}";
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            uri += queryString.StartsWith('?') ? queryString : $"?{queryString}";
        }

        using var response = await SendAuthorizedAsync(HttpMethod.Delete, uri);
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
