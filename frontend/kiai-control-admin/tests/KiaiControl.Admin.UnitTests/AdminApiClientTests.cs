using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Audit;
using KiaiControl.Contracts.Billing;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Contracts.Graduation;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Contracts.Notifications;
using KiaiControl.Contracts.Ranking;
using KiaiControl.Contracts.Reports;

namespace KiaiControl.Admin.UnitTests;

public sealed class AdminApiClientTests
{
    [Fact]
    public async Task ListAsync_ShouldRefreshSessionAndRetryWithTenantContext()
    {
        var organizationId = Guid.NewGuid();
        var session = new AdminSession();
        await session.SetAsync(CreateSession("expired-access", "refresh-token", organizationId));
        var refreshedSession = CreateSession("new-access", "new-refresh-token", organizationId);
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            JsonResponse(refreshedSession),
            JsonResponse(new[] { "client-1" }));
        var api = CreateClient(handler, session);

        var clients = await api.ListAsync<string>("clients");

        Assert.Equal(["client-1"], clients);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal("expired-access", handler.Requests[0].AccessToken);
        Assert.Equal(organizationId.ToString(), handler.Requests[0].OrganizationId);
        Assert.Equal("api/v1/auth/refresh", handler.Requests[1].RequestUri!.PathAndQuery.TrimStart('/'));
        using var refreshRequest = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal("refresh-token", refreshRequest.RootElement.GetProperty("refreshToken").GetString());
        Assert.Equal("new-access", handler.Requests[2].AccessToken);
        Assert.Equal(organizationId.ToString(), handler.Requests[2].OrganizationId);
        Assert.Equal("new-access", session.Current!.AccessToken);
        Assert.Equal("new-refresh-token", session.Current.RefreshToken);
    }

    [Fact]
    public async Task ValidateRestoredSessionAsync_ShouldRefreshExpiredAccessAndValidateUser()
    {
        var organizationId = Guid.NewGuid();
        var session = new AdminSession();
        var persistedSession = CreateSession(
            "expired-access",
            "refresh-token",
            organizationId,
            DateTimeOffset.UtcNow.AddSeconds(-1));
        await session.SetAsync(persistedSession);
        var refreshedSession = CreateSession("new-access", "new-refresh-token", organizationId);
        var handler = new QueueHttpMessageHandler(
            JsonResponse(refreshedSession),
            JsonResponse(new AuthUserProfileResponse
            {
                UserId = refreshedSession.User.UserId,
                PersonId = refreshedSession.User.PersonId,
                OrganizationId = organizationId,
                Email = refreshedSession.User.Email
            }));
        var api = CreateClient(handler, session);

        var isValid = await api.ValidateRestoredSessionAsync();

        Assert.True(isValid);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("api/v1/auth/refresh", handler.Requests[0].RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Equal("new-access", handler.Requests[1].AccessToken);
        Assert.Equal(organizationId.ToString(), handler.Requests[1].OrganizationId);
        Assert.Equal("new-refresh-token", session.Current!.RefreshToken);
    }

    [Fact]
    public async Task ListAsync_ShouldKeepSessionWhenRefreshFailsTemporarily()
    {
        var session = new AdminSession();
        await session.SetAsync(CreateSession("expired-access", "refresh-token", Guid.NewGuid()));
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("API temporarily unavailable")
            });
        var api = CreateClient(handler, session);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => api.ListAsync<string>("clients"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("refresh-token", session.Current!.RefreshToken);
    }

    [Fact]
    public async Task ListAsync_ShouldClearSessionWhenRefreshTokenIsRejected()
    {
        var session = new AdminSession();
        await session.SetAsync(CreateSession("expired-access", "revoked-refresh-token", Guid.NewGuid()));
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var api = CreateClient(handler, session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => api.ListAsync<string>("clients"));

        Assert.Null(session.Current);
    }

    [Fact]
    public async Task ListAsync_ShouldPreserveHttpStatusAndErrorMessage()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("Access denied")
            });
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => api.ListAsync<string>("clients"));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("Access denied", exception.Message);
    }

    [Fact]
    public async Task ClientFinancialHistoryMethods_ShouldRequestRecordsFilteredByClient()
    {
        var clientId = Guid.NewGuid();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(Array.Empty<ClientPlanSubscriptionResponse>()),
            JsonResponse(Array.Empty<BillingResponse>()),
            JsonResponse(Array.Empty<PaymentResponse>()));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.ListClientSubscriptionsAsync(clientId);
        await api.ListClientInvoicesAsync(clientId);
        await api.ListClientPaymentsAsync(clientId);

        Assert.All(handler.Requests, request =>
            Assert.Contains($"clientId={clientId:D}", request.RequestUri!.Query));
        Assert.Contains("/billing/subscriptions?", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Contains("/billing?", handler.Requests[1].RequestUri!.PathAndQuery);
        Assert.Contains("/billing/payments?", handler.Requests[2].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task ReportMethods_ShouldUseDateModalityAndPaginationFilters()
    {
        var modalityId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var handler = new QueueHttpMessageHandler(
            JsonResponse(Array.Empty<ModalityResponse>()),
            JsonResponse(Array.Empty<AttendanceReportRow>()),
            JsonResponse(new FinancialReportResponse()),
            JsonResponse(Array.Empty<ClientPerformanceReportRow>()),
            JsonResponse(Array.Empty<GraduationReportRow>()));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.ListReportModalitiesAsync();
        await api.GetAttendanceReportAsync(from, to, modalityId);
        await api.GetFinancialReportAsync(from, to);
        await api.GetClientPerformanceReportAsync(from, to, modalityId, page: 2, pageSize: 50);
        await api.GetGraduationReportAsync(from, to, modalityId);

        Assert.Contains("/reports/modalities", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Contains("from=2026-01-01", handler.Requests[1].RequestUri!.Query);
        Assert.Contains("to=2026-01-31", handler.Requests[1].RequestUri!.Query);
        Assert.Contains($"modalityId={modalityId:D}", handler.Requests[1].RequestUri!.Query);
        Assert.Contains("/reports/financial?from=2026-01-01&to=2026-01-31", handler.Requests[2].RequestUri!.PathAndQuery);
        Assert.Contains("page=2", handler.Requests[3].RequestUri!.Query);
        Assert.Contains("pageSize=50", handler.Requests[3].RequestUri!.Query);
        Assert.Contains($"modalityId={modalityId:D}", handler.Requests[4].RequestUri!.Query);
    }

    [Fact]
    public async Task GraduationMethods_ShouldCallTenantScopedConfigurationAndPromotionRoutes()
    {
        var clientId = Guid.NewGuid();
        var modalityId = Guid.NewGuid();
        var systemId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(Array.Empty<GraduationSystemResponse>()),
            JsonResponse(new GraduationSystemResponse { Id = systemId, ModalityId = modalityId, Name = "Faixa" }),
            JsonResponse(Array.Empty<GraduationLevelResponse>()),
            JsonResponse(new GraduationLevelResponse { Id = levelId, GraduationSystemId = systemId, Name = "Branca" }),
            JsonResponse(Array.Empty<GraduationBeltResponse>()),
            JsonResponse(new GraduationBeltResponse { Id = Guid.NewGuid(), GraduationSystemId = systemId, Name = "Branca", DegreeCount = 4 }),
            JsonResponse(new GraduationLevelResponse { Id = Guid.NewGuid(), GraduationSystemId = systemId, BeltId = Guid.NewGuid(), BeltName = "Branca", DegreeNumber = 1 }),
            JsonResponse(Array.Empty<EligibleClientResponse>()),
            JsonResponse(new ClientGraduationResponse { ClientId = clientId, ModalityId = modalityId, CurrentLevelId = levelId }),
            JsonResponse(new GraduationEventResponse { ClientId = clientId, ModalityId = modalityId, ToLevelId = levelId }),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            new HttpResponseMessage(HttpStatusCode.NoContent),
            new HttpResponseMessage(HttpStatusCode.NoContent));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.ListGraduationSystemsAsync();
        await api.CreateGraduationSystemAsync(new CreateGraduationSystemRequest { ModalityId = modalityId, Name = "Faixa" });
        await api.ListGraduationLevelsAsync(systemId);
        await api.CreateGraduationLevelAsync(systemId, new CreateGraduationLevelRequest { Name = "Branca", HierarchyOrder = 1 });
        await api.ListGraduationBeltsAsync(systemId);
        await api.CreateGraduationBeltAsync(systemId, new CreateGraduationBeltRequest { Name = "Branca", DegreeCount = 4 });
        await api.CreateGraduationDegreeAsync(systemId, Guid.NewGuid(), new CreateGraduationDegreeRequest { MinLessons = 100 });
        await api.ListEligibleGraduationClientsAsync(systemId);
        await api.SetInitialGraduationAsync(clientId, new SetClientGraduationRequest { ModalityId = modalityId, LevelId = levelId, GrantedAt = new DateOnly(2026, 1, 1) });
        await api.PromoteClientAsync(clientId, new PromoteClientRequest { ModalityId = modalityId, ToLevelId = levelId, EventDate = new DateOnly(2026, 2, 1) });
        var beltId = Guid.NewGuid();
        var degreeId = Guid.NewGuid();
        await api.UpdateGraduationBeltAsync(systemId, beltId, new CreateGraduationBeltRequest { Name = "Branca", DegreeCount = 4 });
        await api.DeleteGraduationBeltAsync(systemId, beltId);
        await api.UpdateGraduationDegreeAsync(systemId, beltId, degreeId, new CreateGraduationDegreeRequest { MinLessons = 100 });
        await api.DeleteGraduationDegreeAsync(systemId, beltId, degreeId);

        Assert.Equal("/api/v1/graduation-systems", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("/api/v1/graduation-systems", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/levels", handler.Requests[2].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/levels", handler.Requests[3].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts", handler.Requests[4].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts", handler.Requests[5].RequestUri!.AbsolutePath);
        Assert.EndsWith("/degrees", handler.Requests[6].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation/eligible-clients?systemId={systemId:D}", handler.Requests[7].RequestUri!.PathAndQuery);
        Assert.Equal($"/api/v1/clients/{clientId}/graduations", handler.Requests[8].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/clients/{clientId}/graduation-events", handler.Requests[9].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts/{beltId}", handler.Requests[10].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts/{beltId}", handler.Requests[11].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts/{beltId}/degrees/{degreeId}", handler.Requests[12].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/graduation-systems/{systemId}/belts/{beltId}/degrees/{degreeId}", handler.Requests[13].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.Requests[10].Method);
        Assert.Equal(HttpMethod.Delete, handler.Requests[11].Method);
        Assert.Equal(HttpMethod.Put, handler.Requests[12].Method);
        Assert.Equal(HttpMethod.Delete, handler.Requests[13].Method);
        Assert.All(handler.Requests, request => Assert.NotNull(request.OrganizationId));
    }

    [Fact]
    public async Task FitnessMethods_ShouldUseScopedOptionsPagingProgressAndCancellationRoutes()
    {
        var clientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(new FitnessOptionsResponse()),
            JsonResponse(Array.Empty<FitnessGoalResponse>()),
            JsonResponse(new FitnessGoalResponse { Id = goalId, ClientId = clientId, Title = "Treino" }),
            JsonResponse(Array.Empty<FitnessProgressResponse>()),
            JsonResponse(new FitnessProgressResponse { FitnessGoalId = goalId, RecordedValue = 2 }),
            new HttpResponseMessage(HttpStatusCode.NoContent));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.GetFitnessOptionsAsync();
        await api.ListFitnessGoalsAsync(page: 2, pageSize: 25, clientId: clientId);
        await api.CreateFitnessGoalAsync(new CreateFitnessGoalRequest
        {
            ClientId = clientId,
            Title = "Treino",
            GoalType = "training_days",
            TargetValue = 12,
            StartsAt = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 2, 1)
        });
        await api.ListFitnessProgressAsync(goalId, page: 2, pageSize: 10);
        await api.RecordFitnessProgressAsync(goalId, new RecordFitnessProgressRequest { RecordedValue = 2 });
        await api.CancelFitnessGoalAsync(goalId);

        Assert.Equal("/api/v1/fitness-goals/options", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("page=2", handler.Requests[1].RequestUri!.Query);
        Assert.Contains("pageSize=25", handler.Requests[1].RequestUri!.Query);
        Assert.Contains($"clientId={clientId:D}", handler.Requests[1].RequestUri!.Query);
        Assert.Equal("/api/v1/fitness-goals", handler.Requests[2].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/fitness-goals/{goalId}/progress", handler.Requests[3].RequestUri!.AbsolutePath);
        Assert.Contains("page=2", handler.Requests[3].RequestUri!.Query);
        Assert.Contains("pageSize=10", handler.Requests[3].RequestUri!.Query);
        Assert.Equal($"/api/v1/fitness-goals/{goalId}/progress", handler.Requests[4].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/fitness-goals/{goalId}/cancel", handler.Requests[5].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RankingMethods_ShouldCallCycleCalculationAndMedalRoutes()
    {
        var clientId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var medalId = Guid.NewGuid();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(new RankingOptionsResponse()),
            JsonResponse(Array.Empty<RankingCycleResponse>()),
            JsonResponse(new RankingCycleResponse { Id = cycleId, Name = "Janeiro" }),
            JsonResponse(Array.Empty<RankingEntryResponse>()),
            JsonResponse(Array.Empty<RankingEntryResponse>()),
            JsonResponse(Array.Empty<RankingEntryResponse>()),
            JsonResponse(Array.Empty<MedalResponse>()),
            JsonResponse(new MedalResponse { Id = medalId, Name = "Consistência" }),
            JsonResponse(new PersonMedalResponse { MedalId = medalId }),
            JsonResponse(Array.Empty<PersonMedalResponse>()));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.GetRankingOptionsAsync();
        await api.ListRankingCyclesAsync(page: 1, pageSize: 20);
        await api.CreateRankingCycleAsync(new CreateRankingCycleRequest { Name = "Janeiro" });
        await api.ListCurrentRankingAsync(page: 1, pageSize: 20);
        await api.ListRankingEntriesAsync(cycleId, page: 1, pageSize: 20);
        await api.RecalculateRankingAsync(cycleId);
        await api.ListMedalsAsync();
        await api.CreateMedalAsync(new CreateMedalRequest { Code = "consistencia", Name = "Consistência" });
        await api.AwardMedalAsync(medalId, new AwardMedalRequest { PersonId = clientId });
        await api.ListClientMedalsAsync(clientId);

        Assert.Equal("/api/v1/rankings/options", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("/api/v1/rankings/history?page=1&pageSize=20", handler.Requests[1].RequestUri!.PathAndQuery);
        Assert.Equal("/api/v1/ranking-cycles", handler.Requests[2].RequestUri!.AbsolutePath);
        Assert.Contains("/api/v1/rankings/current?page=1&pageSize=20", handler.Requests[3].RequestUri!.PathAndQuery);
        Assert.Contains($"/api/v1/ranking-cycles/{cycleId}/entries?page=1&pageSize=20", handler.Requests[4].RequestUri!.PathAndQuery);
        Assert.Equal($"/api/v1/ranking-cycles/{cycleId}/recalculate", handler.Requests[5].RequestUri!.AbsolutePath);
        Assert.Equal("/api/v1/medals", handler.Requests[6].RequestUri!.AbsolutePath);
        Assert.Equal("/api/v1/medals", handler.Requests[7].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/medals/{medalId}/awards", handler.Requests[8].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/clients/{clientId}/medals", handler.Requests[9].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task NotificationMethods_ShouldSeparateInboxAndOutboxAndMarkOwnMessageRead()
    {
        var notificationId = Guid.NewGuid();
        var handler = new QueueHttpMessageHandler(
            JsonResponse(new NotificationOptionsResponse()),
            JsonResponse(Array.Empty<NotificationResponse>()),
            JsonResponse(Array.Empty<NotificationResponse>()),
            JsonResponse(new NotificationResponse { Id = notificationId, Title = "Aviso" }),
            new HttpResponseMessage(HttpStatusCode.NoContent));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.GetNotificationOptionsAsync();
        await api.ListMyNotificationsAsync(page: 1, pageSize: 20);
        await api.ListNotificationOutboxAsync(page: 1, pageSize: 20);
        await api.CreateNotificationAsync(new CreateNotificationRequest
        {
            PersonId = Guid.NewGuid(),
            Title = "Aviso",
            Body = "Mensagem",
            Channel = "in_app"
        });
        await api.MarkNotificationAsReadAsync(notificationId);

        Assert.Equal("/api/v1/notifications/options", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("/api/v1/notifications?page=1&pageSize=20", handler.Requests[1].RequestUri!.PathAndQuery);
        Assert.Contains("/api/v1/notifications/outbox?page=1&pageSize=20", handler.Requests[2].RequestUri!.PathAndQuery);
        Assert.Equal("/api/v1/notifications", handler.Requests[3].RequestUri!.AbsolutePath);
        Assert.Equal($"/api/v1/notifications/{notificationId}/read", handler.Requests[4].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AuditLogMethod_ShouldSendUtcRangeAndPagination()
    {
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var handler = new QueueHttpMessageHandler(JsonResponse(Array.Empty<AuditLogResponse>()));
        var session = new AdminSession();
        await session.SetAsync(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        await api.ListAuditLogsAsync(from, to, page: 3, pageSize: 25);

        var query = Uri.UnescapeDataString(handler.Requests.Single().RequestUri!.Query);
        Assert.Equal("/api/v1/audit-logs", handler.Requests.Single().RequestUri!.AbsolutePath);
        Assert.Contains("page=3", query);
        Assert.Contains("pageSize=25", query);
        Assert.Contains($"from={from:O}", query);
        Assert.Contains($"to={to:O}", query);
    }

    private static AdminApiClient CreateClient(QueueHttpMessageHandler handler, AdminSession session) =>
        new(new StubHttpClientFactory(handler), session);

    private static SessionTokenResponse CreateSession(
        string accessToken,
        string refreshToken,
        Guid organizationId,
        DateTimeOffset? accessTokenExpiresAt = null) => new()
    {
        AccessToken = accessToken,
        AccessTokenExpiresAt = accessTokenExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15),
        RefreshToken = refreshToken,
        RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        User = new AuthUserProfileResponse
        {
            UserId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = "admin@example.test",
            Roles = ["owner"]
        }
    };

    private static HttpResponseMessage JsonResponse<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(value)
    };

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.test/")
        };
    }

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var organizationId = request.Headers.TryGetValues("X-Organization-Id", out var values)
                ? values.Single()
                : null;

            Requests.Add(new RecordedRequest(
                request.RequestUri,
                request.Method,
                request.Headers.Authorization?.Scheme == "Bearer" ? request.Headers.Authorization.Parameter : null,
                organizationId,
                body));

            return _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(Uri? RequestUri, HttpMethod Method, string? AccessToken, string? OrganizationId, string? Body);
}