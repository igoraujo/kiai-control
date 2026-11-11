using System.Security.Claims;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Api.Services;

public sealed class PlatformIncidentRecorder(IPlatformAdministrationRepository repository, ILogger<PlatformIncidentRecorder> logger)
{
    public async Task TryRecordAsync(HttpContext context, string category, int statusCode)
    {
        // Route templates deliberately exclude values, query strings, headers and request bodies.
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "/unmatched";
        var organizationId = Guid.TryParse(context.User.FindFirstValue("organization_id"), out var org) ? org : (Guid?)null;
        var actorUserId = Guid.TryParse(context.User.FindFirstValue("sub"), out var user) ? user : (Guid?)null;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await repository.RecordIncidentAsync(new PlatformIncident
            {
                OrganizationId = organizationId, ActorUserId = actorUserId, Category = category,
                Method = context.Request.Method[..Math.Min(context.Request.Method.Length, 16)],
                Route = route[..Math.Min(route.Length, 300)], StatusCode = statusCode,
                CorrelationId = context.TraceIdentifier[..Math.Min(context.TraceIdentifier.Length, 120)]
            }, timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist platform incident {Category}. CorrelationId: {CorrelationId}", category, context.TraceIdentifier);
        }
    }
}
