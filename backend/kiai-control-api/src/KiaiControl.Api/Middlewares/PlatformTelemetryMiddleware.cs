using System.Diagnostics;
using System.Security.Claims;
using KiaiControl.Api.Services;
using KiaiControl.Core.Entities;

namespace KiaiControl.Api.Middlewares;

public sealed class PlatformTelemetryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PlatformTelemetryBuffer buffer)
    {
        var start = Stopwatch.GetTimestamp();
        await next(context);
        if (!context.Request.Path.StartsWithSegments("/api/v1")) return;
        var now = DateTimeOffset.UtcNow;
        var duration = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "/unmatched";
        Guid? organization = context.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(context.User.FindFirstValue("organization_id"), out var id) ? id : null;
        if (context.User.IsInRole("platform_owner")
            && context.Request.RouteValues.TryGetValue("organizationId", out var target)
            && Guid.TryParse(target?.ToString(), out var targetId)) organization = targetId;
        var method = context.Request.Method.ToUpperInvariant();
        if (method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS")) method = "OTHER";
        buffer.Add(new PlatformRequestMetric
        {
            BucketStart = new(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero),
            OrganizationId = organization, Route = route[..Math.Min(route.Length, 300)], Method = method,
            StatusCode = Math.Clamp(context.Response.StatusCode, 100, 599), Requests = 1,
            DurationTotalMs = duration, MaxDurationMs = duration
        });
    }
}
