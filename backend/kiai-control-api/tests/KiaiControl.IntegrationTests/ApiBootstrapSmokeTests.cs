using System.Security.Claims;
using KiaiControl.Contracts.Auth;
using KiaiControl.Api.Extensions;
using KiaiControl.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace KiaiControl.IntegrationTests;

public sealed class ApiBootstrapRegistrationTests
{
    [Fact]
    public async Task AddKiaiApi_ShouldRegisterRepositoriesAndEnforcePermissionClaimsWithoutOpeningConnections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = "Host=localhost;Database=kiai_test;Username=test;Password=test",
                ["Jwt:Issuer"] = "KiaiControl.Tests",
                ["Jwt:Audience"] = "KiaiControl.Tests.Clients",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKiaiApi(configuration, new TestHostEnvironment());

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IClientRepository>());
        Assert.NotNull(provider.GetRequiredService<IAuditLogRepository>());

        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
        var permittedUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(PermissionCodes.ClaimType, PermissionCodes.ClientsManage)
        ], "test"));

        Assert.False((await authorization.AuthorizeAsync(authenticatedUser, PermissionCodes.ClientsManage)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(permittedUser, PermissionCodes.ClientsManage)).Succeeded);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "KiaiControl.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
