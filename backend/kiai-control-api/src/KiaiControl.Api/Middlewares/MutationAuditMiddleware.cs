using System.Security.Claims;
using System.Text.Json;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Api.Middlewares;

public sealed class MutationAuditMiddleware(RequestDelegate next, ILogger<MutationAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, OrganizationContext organizationContext, IAuditLogRepository auditLogRepository)
    {
        await next(context);

        if (!context.User.Identity?.IsAuthenticated == true
            || organizationContext.OrganizationId is not Guid organizationId
            || context.Response.StatusCode is < 200 or >= 300
            || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method))
        {
            return;
        }

        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (pathSegments.Length < 3 || !string.Equals(pathSegments[0], "api", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var entityId = pathSegments
            .Select(segment => Guid.TryParse(segment, out var id) ? id : (Guid?)null)
            .FirstOrDefault(id => id is not null);
        var metadata = JsonSerializer.Serialize(new
        {
            method = context.Request.Method,
            route = context.Request.Path.Value,
            statusCode = context.Response.StatusCode,
            correlationId = context.TraceIdentifier
        });

        try
        {
            await auditLogRepository.RecordAsync(new AuditLog
            {
                OrganizationId = organizationId,
                ActorPersonId = ParseClaim(context.User, "person_id"),
                ActorUserAccountId = ParseClaim(context.User, ClaimTypes.NameIdentifier)
                    ?? ParseClaim(context.User, "sub"),
                Action = context.Request.Method.ToUpperInvariant(),
                EntityName = pathSegments[2],
                EntityId = entityId,
                Metadata = metadata,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString() is { Length: > 0 } userAgent
                    ? userAgent[..Math.Min(userAgent.Length, 255)]
                    : null
            }, context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to persist mutation audit event for {Route}.", context.Request.Path);
        }
    }

    private static Guid? ParseClaim(ClaimsPrincipal principal, string claimType)
    {
        return Guid.TryParse(principal.FindFirstValue(claimType), out var value) ? value : null;
    }
}