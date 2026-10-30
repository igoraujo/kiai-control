using System.Net;
using Bunit;
using KiaiControl.Admin.Components.Pages.Access;
using KiaiControl.Admin.Services;
using KiaiControl.Contracts.Auth;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace KiaiControl.Admin.UnitTests;

public sealed class AccessControlPageTests
{
    [Fact]
    public async Task AccessControl_ShouldNotLoadDataWithoutPermission()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = new AdminSession();
        session.Set(new SessionTokenResponse
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            User = new AuthUserProfileResponse
            {
                UserId = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Email = "admin@example.test",
                Roles = ["admin"],
                Permissions = []
            }
        });
        var handler = new RecordingHttpMessageHandler();
        context.Services.AddSingleton(session);
        context.Services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(handler));
        context.Services.AddSingleton<AdminApiClient>();

        var page = context.Render<AccessControl>();

        Assert.Contains("Seu perfil não pode administrar permissões de roles.", page.Markup);
        Assert.Empty(handler.Requests);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.test/")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }
    }
}