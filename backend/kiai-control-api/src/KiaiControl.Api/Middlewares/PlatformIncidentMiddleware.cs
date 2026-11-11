using KiaiControl.Api.Services;

namespace KiaiControl.Api.Middlewares;

public sealed class PlatformIncidentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformIncidentRecorder recorder)
    {
        await next(context);
        if (context.Response.StatusCode >= 500)
            await recorder.TryRecordAsync(context, "request_failure", context.Response.StatusCode);
        else if (context.Response.StatusCode == 401 && context.Request.Path.Equals("/api/v1/auth/login"))
            await recorder.TryRecordAsync(context, "authentication_failure", 401);
    }
}
