using System.Diagnostics;

namespace KiaiControl.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incomingCorrelationId)
            && !string.IsNullOrWhiteSpace(incomingCorrelationId)
            ? incomingCorrelationId.ToString()
            : Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlation_id"] = correlationId
        }))
        {
            await next(context);
        }
    }
}
