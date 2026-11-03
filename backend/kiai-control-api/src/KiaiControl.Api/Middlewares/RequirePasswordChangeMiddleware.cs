using System.Security.Claims;
using KiaiControl.Api.Common;

namespace KiaiControl.Api.Middlewares;

public sealed class RequirePasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requiresPasswordChange = context.User.Identity?.IsAuthenticated == true
            && bool.TryParse(context.User.FindFirstValue("must_change_password"), out var mustChange)
            && mustChange;
        var isAllowedAuthEndpoint = context.Request.Path.Equals("/api/v1/auth/change-password")
            || context.Request.Path.Equals("/api/v1/auth/logout");

        if (requiresPasswordChange && !isAllowedAuthEndpoint)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                title = SystemMessages.ChangeTemporaryPasswordBeforeProceeding,
                status = StatusCodes.Status403Forbidden
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
