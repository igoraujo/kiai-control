using System.Net;
using System.Net.Http.Json;
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
        await session.SetAsync(new SessionTokenResponse
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

        Assert.Contains("Seu perfil não pode administrar contas de acesso.", page.Markup);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task AccessControl_ShouldLoadRolesAndPermissionsForAuthorizedUser()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = CreateSession(PermissionCodes.AccessManage, PermissionCodes.AccessPermissionsManage);
        var handler = new RecordingHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/roles", StringComparison.Ordinal))
            {
                return JsonResponse(new[] { new RoleResponse { Code = "owner", Name = "Proprietário" } });
            }

            if (path.EndsWith("/people/without-account", StringComparison.Ordinal))
            {
                return JsonResponse(Array.Empty<UserAccountCandidateResponse>());
            }

            if (path.EndsWith("/access/permissions", StringComparison.Ordinal))
            {
                return JsonResponse(new[] { new PermissionResponse { Code = PermissionCodes.BillingManage, Name = "Gerenciar financeiro" } });
            }

            return JsonResponse(new[] { new PermissionResponse { Code = PermissionCodes.BillingManage, Name = "Gerenciar financeiro" } });
        });
        RegisterServices(context, session, handler);

        var page = context.Render<AccessControl>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Gerenciar financeiro", page.Markup);
            Assert.Contains("As permissões do owner são fixas.", page.Markup);
        });
        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public async Task AccessControl_ShouldShowLoadingWhileRequestsArePending()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = CreateSession(PermissionCodes.AccessManage, PermissionCodes.AccessPermissionsManage);
        var responseSource = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestNumber = 0;
        var handler = new RecordingHttpMessageHandler(_ =>
            Interlocked.Increment(ref requestNumber) == 1
                ? responseSource.Task
                : JsonResponse(Array.Empty<PermissionResponse>()));
        RegisterServices(context, session, handler);

        var page = context.Render<AccessControl>();

        Assert.Contains("mud-progress-linear", page.Markup);
        responseSource.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { new RoleResponse { Code = "owner", Name = "Proprietário" } })
        });
        page.WaitForAssertion(() => Assert.Contains("Nenhuma permissão cadastrada.", page.Markup));
    }

    [Fact]
    public async Task AccessControl_ShouldDisplayApiError()
    {
        await using var context = new BunitContext();
        context.Services.AddMudServices();
        var session = CreateSession(PermissionCodes.AccessManage, PermissionCodes.AccessPermissionsManage);
        var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("Acesso negado")
        }));
        RegisterServices(context, session, handler);

        var page = context.Render<AccessControl>();

        page.WaitForAssertion(() => Assert.Contains("Acesso negado", page.Markup));
    }

    private static AdminSession CreateSession(params string[] permissions)
    {
        var session = new AdminSession();
        session.SetAsync(new SessionTokenResponse
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
                Roles = ["owner"],
                Permissions = permissions
            }
        }).GetAwaiter().GetResult();
        return session;
    }

    private static void RegisterServices(BunitContext context, AdminSession session, RecordingHttpMessageHandler handler)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(session);
        context.Services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(handler));
        context.Services.AddSingleton<AdminApiClient>();
    }

    private static Task<HttpResponseMessage> JsonResponse<T>(T value) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(value)
    });

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.test/")
        };
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? respond = null) : HttpMessageHandler
    {
        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);
            return respond?.Invoke(request)
                ?? Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }
    }
}