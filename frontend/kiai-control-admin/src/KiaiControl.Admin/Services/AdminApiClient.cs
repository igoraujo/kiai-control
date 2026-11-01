using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Audit;
using KiaiControl.Contracts.Attendance;
using KiaiControl.Contracts.Billing;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Contracts.Graduation;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Contracts.Notifications;
using KiaiControl.Contracts.Ranking;
using KiaiControl.Contracts.Reports;
using KiaiControl.Contracts.Teachers;

namespace KiaiControl.Admin.Services;

public sealed class AdminApiClient(IHttpClientFactory httpClientFactory, AdminSession session)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

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

    public async Task<bool> ValidateRestoredSessionAsync()
    {
        var currentSession = session.Current;
        if (currentSession is null)
        {
            return false;
        }

        if (currentSession.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            await session.ClearAsync();
            return false;
        }

        if (currentSession.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(30)
            && !await RefreshSessionAsync(currentSession))
        {
            return false;
        }

        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/auth/me");
        return response.IsSuccessStatusCode;
    }

    public async Task<DashboardReportResponse> GetDashboardAsync(DateOnly from, DateOnly to)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            $"api/v1/reports/dashboard?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return await response.Content.ReadFromJsonAsync<DashboardReportResponse>()
            ?? throw new HttpRequestException("The API returned an empty dashboard response.");
    }

    public Task<IReadOnlyList<ModalityResponse>> ListReportModalitiesAsync() =>
        GetListAsync<ModalityResponse>("api/v1/reports/modalities");

    public Task<IReadOnlyList<AttendanceReportRow>> GetAttendanceReportAsync(
        DateOnly from,
        DateOnly to,
        Guid? modalityId) =>
        GetListAsync<AttendanceReportRow>(
            $"api/v1/reports/attendance?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}{ModalityQuery(modalityId)}");

    public async Task<FinancialReportResponse> GetFinancialReportAsync(DateOnly from, DateOnly to)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get,
            $"api/v1/reports/financial?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return await response.Content.ReadFromJsonAsync<FinancialReportResponse>()
            ?? throw new HttpRequestException("The API returned an empty financial report response.");
    }

    public Task<IReadOnlyList<ClientPerformanceReportRow>> GetClientPerformanceReportAsync(
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        int page,
        int pageSize) =>
        GetListAsync<ClientPerformanceReportRow>(
            $"api/v1/reports/client-performance?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}{ModalityQuery(modalityId)}&page={page}&pageSize={pageSize}");

    public Task<IReadOnlyList<GraduationReportRow>> GetGraduationReportAsync(
        DateOnly from,
        DateOnly to,
        Guid? modalityId) =>
        GetListAsync<GraduationReportRow>(
            $"api/v1/reports/graduation?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}{ModalityQuery(modalityId)}");

    public Task<IReadOnlyList<GraduationSystemResponse>> ListGraduationSystemsAsync() =>
        GetListAsync<GraduationSystemResponse>("api/v1/graduation-systems");

    public async Task<GraduationOptionsResponse> GetGraduationOptionsAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/graduation/options");
        return await response.Content.ReadFromJsonAsync<GraduationOptionsResponse>()
            ?? throw new HttpRequestException("The API returned empty graduation options.");
    }

    public Task<GraduationSystemResponse> CreateGraduationSystemAsync(CreateGraduationSystemRequest request) =>
        CreateAsync<CreateGraduationSystemRequest, GraduationSystemResponse>("graduation-systems", request);

    public Task<IReadOnlyList<GraduationLevelResponse>> ListGraduationLevelsAsync(Guid systemId) =>
        GetListAsync<GraduationLevelResponse>($"api/v1/graduation-systems/{systemId}/levels");

    public Task<GraduationLevelResponse> CreateGraduationLevelAsync(
        Guid systemId,
        CreateGraduationLevelRequest request) =>
        CreateAsync<CreateGraduationLevelRequest, GraduationLevelResponse>(
            $"graduation-systems/{systemId}/levels", request);

    public Task<IReadOnlyList<EligibleClientResponse>> ListEligibleGraduationClientsAsync(Guid systemId) =>
        GetListAsync<EligibleClientResponse>(
            $"api/v1/graduation/eligible-clients?systemId={systemId:D}");

    public Task<ClientGraduationResponse> SetInitialGraduationAsync(
        Guid clientId,
        SetClientGraduationRequest request) =>
        CreateAsync<SetClientGraduationRequest, ClientGraduationResponse>(
            $"clients/{clientId}/graduations", request);

    public Task<GraduationEventResponse> PromoteClientAsync(
        Guid clientId,
        PromoteClientRequest request) =>
        CreateAsync<PromoteClientRequest, GraduationEventResponse>(
            $"clients/{clientId}/graduation-events", request);

    public async Task<FitnessOptionsResponse> GetFitnessOptionsAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/fitness-goals/options");
        return await response.Content.ReadFromJsonAsync<FitnessOptionsResponse>()
            ?? throw new HttpRequestException("The API returned empty fitness options.");
    }

    public Task<IReadOnlyList<FitnessGoalResponse>> ListFitnessGoalsAsync(
        int page = 1,
        int pageSize = 50,
        Guid? clientId = null) =>
        ListAsync<FitnessGoalResponse>("fitness-goals", page, pageSize, clientId);

    public Task<FitnessGoalResponse> CreateFitnessGoalAsync(CreateFitnessGoalRequest request) =>
        CreateAsync<CreateFitnessGoalRequest, FitnessGoalResponse>("fitness-goals", request);

    public Task<FitnessProgressResponse> RecordFitnessProgressAsync(
        Guid goalId,
        RecordFitnessProgressRequest request) =>
        CreateAsync<RecordFitnessProgressRequest, FitnessProgressResponse>(
            $"fitness-goals/{goalId}/progress", request);

    public async Task CancelFitnessGoalAsync(Guid goalId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/fitness-goals/{goalId}/cancel");
    }

    public async Task<RankingOptionsResponse> GetRankingOptionsAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/rankings/options");
        return await response.Content.ReadFromJsonAsync<RankingOptionsResponse>()
            ?? throw new HttpRequestException("The API returned empty ranking options.");
    }

    public Task<IReadOnlyList<RankingCycleResponse>> ListRankingCyclesAsync(int page = 1, int pageSize = 50) =>
        ListAsync<RankingCycleResponse>("rankings/history", page, pageSize);

    public Task<RankingCycleResponse> CreateRankingCycleAsync(CreateRankingCycleRequest request) =>
        CreateAsync<CreateRankingCycleRequest, RankingCycleResponse>("ranking-cycles", request);

    public Task<IReadOnlyList<RankingEntryResponse>> ListCurrentRankingAsync(int page = 1, int pageSize = 50) =>
        ListAsync<RankingEntryResponse>("rankings/current", page, pageSize);

    public Task<IReadOnlyList<RankingEntryResponse>> ListRankingEntriesAsync(
        Guid cycleId,
        int page = 1,
        int pageSize = 50) =>
        ListAsync<RankingEntryResponse>($"ranking-cycles/{cycleId}/entries", page, pageSize);

    public async Task<IReadOnlyList<RankingEntryResponse>> RecalculateRankingAsync(Guid cycleId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, $"api/v1/ranking-cycles/{cycleId}/recalculate");
        return await response.Content.ReadFromJsonAsync<List<RankingEntryResponse>>()
            ?? throw new HttpRequestException("The API returned an empty ranking response.");
    }

    public Task<IReadOnlyList<MedalResponse>> ListMedalsAsync() =>
        GetListAsync<MedalResponse>("api/v1/medals");

    public Task<MedalResponse> CreateMedalAsync(CreateMedalRequest request) =>
        CreateAsync<CreateMedalRequest, MedalResponse>("medals", request);

    public Task<PersonMedalResponse> AwardMedalAsync(Guid medalId, AwardMedalRequest request) =>
        CreateAsync<AwardMedalRequest, PersonMedalResponse>($"medals/{medalId}/awards", request);

    public Task<IReadOnlyList<PersonMedalResponse>> ListClientMedalsAsync(Guid clientId) =>
        GetListAsync<PersonMedalResponse>($"api/v1/clients/{clientId}/medals");

    public Task<IReadOnlyList<NotificationResponse>> ListMyNotificationsAsync(int page = 1, int pageSize = 50) =>
        ListAsync<NotificationResponse>("notifications", page, pageSize);

    public Task<IReadOnlyList<NotificationResponse>> ListNotificationOutboxAsync(int page = 1, int pageSize = 50) =>
        GetListAsync<NotificationResponse>($"api/v1/notifications/outbox?page={page}&pageSize={pageSize}");

    public async Task<NotificationOptionsResponse> GetNotificationOptionsAsync()
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, "api/v1/notifications/options");
        return await response.Content.ReadFromJsonAsync<NotificationOptionsResponse>()
            ?? throw new HttpRequestException("The API returned empty notification options.");
    }

    public Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request) =>
        CreateAsync<CreateNotificationRequest, NotificationResponse>("notifications", request);

    public async Task MarkNotificationAsReadAsync(Guid notificationId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Patch, $"api/v1/notifications/{notificationId}/read");
    }

    public Task<IReadOnlyList<AuditLogResponse>> ListAuditLogsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page = 1,
        int pageSize = 50)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (from is not null)
        {
            query += $"&from={Uri.EscapeDataString(from.Value.ToString("O"))}";
        }
        if (to is not null)
        {
            query += $"&to={Uri.EscapeDataString(to.Value.ToString("O"))}";
        }

        return GetListAsync<AuditLogResponse>($"api/v1/audit-logs{query}");
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

    public Task<IReadOnlyList<ClientPlanSubscriptionResponse>> ListClientSubscriptionsAsync(
        Guid clientId,
        int page = 1,
        int pageSize = 200) =>
        ListAsync<ClientPlanSubscriptionResponse>("billing/subscriptions", page, pageSize, clientId);

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

    public Task<IReadOnlyList<BillingResponse>> ListClientInvoicesAsync(Guid clientId, int page = 1, int pageSize = 200) =>
        ListAsync<BillingResponse>("billing", page, pageSize, clientId);

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

    public Task<IReadOnlyList<PaymentResponse>> ListClientPaymentsAsync(Guid clientId, int page = 1, int pageSize = 200) =>
        ListAsync<PaymentResponse>("billing/payments", page, pageSize, clientId);

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

    public async Task<IReadOnlyList<ModalityScheduleResponse>> ListTeacherSchedulesAsync(Guid teacherId) =>
        await GetAsync<List<ModalityScheduleResponse>>($"teachers/{teacherId}/schedules");

    public Task<ModalityScheduleResponse> CreateTeacherScheduleAsync(
        Guid teacherId,
        CreateTeacherScheduleRequest request) =>
        CreateAsync<CreateTeacherScheduleRequest, ModalityScheduleResponse>(
            $"teachers/{teacherId}/schedules", request);

    public Task DeleteTeacherScheduleAsync(Guid teacherId, Guid scheduleId) =>
        DeleteAsync($"teachers/{teacherId}/schedules", scheduleId);

    public async Task<IReadOnlyList<LessonResponse>> ListTeacherLessonsAsync(
        Guid teacherId,
        DateTimeOffset from,
        DateTimeOffset to,
        int page = 1,
        int pageSize = 200)
    {
        var fromQuery = Uri.EscapeDataString(from.ToString("O"));
        var toQuery = Uri.EscapeDataString(to.ToString("O"));
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"api/v1/teachers/{teacherId}/lessons?page={page}&pageSize={pageSize}&from={fromQuery}&to={toQuery}");
        return await response.Content.ReadFromJsonAsync<List<LessonResponse>>() ?? [];
    }

    public async Task<IReadOnlyList<LessonResponse>> GenerateModalityLessonsAsync(GenerateModalityLessonsRequest request)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Post, "api/v1/lessons/generate-from-modality", request);
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

    public async Task<IReadOnlyList<EligibleAttendanceClientResponse>> GetEligibleLessonClientsAsync(Guid lessonId)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, $"api/v1/lessons/{lessonId}/eligible-clients");
        return await response.Content.ReadFromJsonAsync<List<EligibleAttendanceClientResponse>>() ?? [];
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
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "api/v1/attendance/check-in",
            new CheckInRequest { LessonId = lessonId });
        return await response.Content.ReadFromJsonAsync<AttendanceRecordResponse>()
            ?? throw new HttpRequestException("A API retornou uma resposta vazia.");
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(
        string resource,
        int page = 1,
        int pageSize = 100,
        Guid? clientId = null)
    {
        var clientFilter = clientId is null ? string.Empty : $"&clientId={clientId.Value:D}";
        using var response = await SendAuthorizedAsync(
            HttpMethod.Get, $"api/v1/{resource}?page={page}&pageSize={pageSize}{clientFilter}");
        return await response.Content.ReadFromJsonAsync<List<T>>() ?? [];
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string requestUri)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, requestUri);
        return await response.Content.ReadFromJsonAsync<List<T>>()
            ?? throw new HttpRequestException("The API returned an empty list response.");
    }

    private static string ModalityQuery(Guid? modalityId) =>
        modalityId is null ? string.Empty : $"&modalityId={modalityId.Value:D}";

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
            await session.ClearAsync();
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("KiaiApi");
            using var response = await client.PostAsJsonAsync("api/v1/auth/logout", new LogoutRequest
            {
                RefreshToken = currentSession.RefreshToken
            });
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            await session.ClearAsync();
        }
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
            if (await RefreshSessionAsync(currentSession))
            {
                return await SendAuthorizedAsync(method, requestUri, body, allowRefresh: false);
            }

            await session.ClearAsync();
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await session.ClearAsync();
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

    private async Task<bool> RefreshSessionAsync(SessionTokenResponse observedSession)
    {
        await _refreshLock.WaitAsync();
        try
        {
            var currentSession = session.Current;
            if (currentSession is null)
            {
                return false;
            }

            if (!string.Equals(currentSession.RefreshToken, observedSession.RefreshToken, StringComparison.Ordinal))
            {
                return currentSession.AccessTokenExpiresAt > DateTimeOffset.UtcNow;
            }

            if (currentSession.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
            {
                await session.ClearAsync();
                return false;
            }

            var client = httpClientFactory.CreateClient("KiaiApi");
            using var response = await client.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest
            {
                RefreshToken = currentSession.RefreshToken
            });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await session.ClearAsync();
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                var detail = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
                throw new HttpRequestException(
                    string.IsNullOrWhiteSpace(detail) ? $"Erro {(int)response.StatusCode} ao renovar a sessão." : detail,
                    null,
                    response.StatusCode);
            }

            var refreshed = await response.Content.ReadFromJsonAsync<SessionTokenResponse>()
                ?? throw new HttpRequestException("A API retornou uma sessão vazia ao renovar os tokens.");
            await session.SetAsync(refreshed);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
