using System.Security.Claims;
using System.Text.Json;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Platform;

namespace KiaiControl.Api.Services;

public sealed class PlatformAccessAudit(IHttpContextAccessor accessor, IAuditLogRepository repository)
{
    public AuditLog Create(string action, string entity, Guid? entityId = null, string? reason = null, object? filters = null)
    {
        var context = accessor.HttpContext ?? throw new InvalidOperationException("Request context is required.");
        return new AuditLog
        {
            OrganizationId = PlatformIdentity.OrganizationId,
            ActorPersonId = Guid.TryParse(context.User.FindFirstValue("person_id"), out var personId) ? personId : null,
            ActorUserAccountId = Guid.TryParse(context.User.FindFirstValue("sub"), out var userId) ? userId : null,
            Action = action, EntityName = entity, EntityId = entityId,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Metadata = JsonSerializer.Serialize(new
            {
                reason = reason?.Trim(),
                filters = filters is null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(PlatformAuditPrivacy.SafeMetadata(JsonSerializer.Serialize(filters))),
                correlationId = context.TraceIdentifier
            })
        };
    }

    public Task RecordReadAsync(string entity, object? filters, CancellationToken ct, Guid? organizationId = null)
    {
        var entry = Create("platform.read", entity, filters: filters);
        entry.OrganizationId = organizationId ?? PlatformIdentity.OrganizationId;
        return repository.RecordAsync(entry, ct);
    }

    public Task RecordDetailsAsync(Guid id, Guid organizationId, CancellationToken ct)
    {
        var entry = Create("platform.audit.details_viewed", "platform.audit", id);
        entry.OrganizationId = organizationId;
        return repository.RecordAsync(entry, ct);
    }
}
