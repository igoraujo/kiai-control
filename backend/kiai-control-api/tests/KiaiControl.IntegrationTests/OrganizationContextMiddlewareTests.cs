using System.Security.Claims;
using KiaiControl.Api.Middlewares;
using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Http;

namespace KiaiControl.IntegrationTests;

public sealed class OrganizationContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPreferAuthenticatedOrganizationClaimOverHeader()
    {
        var claimedOrganizationId = Guid.NewGuid();
        var headerOrganizationId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("organization_id", claimedOrganizationId.ToString())
        ], "test"));
        httpContext.Request.Headers["X-Organization-Id"] = headerOrganizationId.ToString();
        var organizationContext = new OrganizationContext();
        var middleware = new OrganizationContextMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(httpContext, organizationContext);

        Assert.Equal(claimedOrganizationId, organizationContext.OrganizationId);
    }
}