using System.Security.Claims;
using KiaiControl.Core.Common;

namespace KiaiControl.Api.Middlewares;

public sealed class OrganizationContextMiddleware(RequestDelegate next)
{
    private const string OrganizationHeaderName = "X-Organization-Id";
    private const string OrganizationClaimName = "organization_id";

    public async Task InvokeAsync(HttpContext context, OrganizationContext organizationContext)
    {
        if (TryResolveOrganizationId(context, out var organizationId))
        {
            organizationContext.OrganizationId = organizationId;
        }

        await next(context);
    }

    private static bool TryResolveOrganizationId(HttpContext context, out Guid organizationId)
    {
        var claimValue = context.User.FindFirstValue(OrganizationClaimName);
        if (Guid.TryParse(claimValue, out organizationId))
        {
            return true;
        }

        var headerValue = context.Request.Headers[OrganizationHeaderName].ToString();
        return Guid.TryParse(headerValue, out organizationId);
    }
}
