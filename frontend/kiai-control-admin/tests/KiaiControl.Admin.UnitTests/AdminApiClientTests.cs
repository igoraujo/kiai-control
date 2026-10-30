using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;

namespace KiaiControl.Admin.UnitTests;

public sealed class AdminApiClientTests
{
    [Fact]
    public async Task ListAsync_ShouldRefreshSessionAndRetryWithTenantContext()
    {
        var organizationId = Guid.NewGuid();
        var session = new AdminSession();
        session.Set(CreateSession("expired-access", "refresh-token", organizationId));
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
    public async Task ListAsync_ShouldPreserveHttpStatusAndErrorMessage()
    {
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("Access denied")
            });
        var session = new AdminSession();
        session.Set(CreateSession("access-token", "refresh-token", Guid.NewGuid()));
        var api = CreateClient(handler, session);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => api.ListAsync<string>("clients"));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("Access denied", exception.Message);
    }

    private static AdminApiClient CreateClient(QueueHttpMessageHandler handler, AdminSession session) =>
        new(new StubHttpClientFactory(handler), session);

    private static SessionTokenResponse CreateSession(string accessToken, string refreshToken, Guid organizationId) => new()
    {
        AccessToken = accessToken,
        AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
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
                request.Headers.Authorization?.Scheme == "Bearer" ? request.Headers.Authorization.Parameter : null,
                organizationId,
                body));

            return _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(Uri? RequestUri, string? AccessToken, string? OrganizationId, string? Body);
}