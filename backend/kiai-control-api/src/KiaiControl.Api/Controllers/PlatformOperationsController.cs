using KiaiControl.Api.Services;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Platform;
using KiaiControl.Core.Entities;
using KiaiControl.UseCases.Platform;
using KiaiControl.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "platform_owner")]
[Route("api/v1/platform")]
[TypeFilter(typeof(PlatformExceptionFilter))]
public sealed class PlatformOperationsController(PlatformAdministrationUseCase platform, PlatformAccessAudit audit,
    PlatformObservabilityUseCase metrics, HealthCheckService healthChecks, IOptions<PlatformPrivacyOptions> privacy) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<PlatformMetricsResponse>> MetricsAsync([FromQuery] Guid? organizationId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var filter = Filter(organizationId, null, null, from, to, page, pageSize);
        var result = await metrics.DashboardAsync(filter, ct);
        await audit.RecordReadAsync("platform.metrics", filter, ct);
        return Ok(result);
    }

    [HttpGet("health")]
    public async Task<ActionResult<PlatformHealthResponse>> HealthAsync(CancellationToken ct)
    {
        var report = await healthChecks.CheckHealthAsync(x => x.Tags.Contains("ready"), ct);
        await audit.RecordReadAsync("platform.health", null, ct);
        return Ok(new PlatformHealthResponse(report.Status.ToString(), DateTimeOffset.UtcNow,
            report.Entries.Select(x => new PlatformDependencyResponse(x.Key, x.Value.Status.ToString(), x.Value.Duration.TotalMilliseconds)).ToArray()));
    }

    [HttpGet("retention-policy")]
    public async Task<ActionResult<PlatformRetentionPolicyResponse>> RetentionPolicyAsync(CancellationToken ct)
    {
        await audit.RecordReadAsync("platform.privacy", null, ct);
        var p = privacy.Value;
        return Ok(new PlatformRetentionPolicyResponse(p.MetricsRetentionDays, p.IncidentRetentionDays, p.AuditIdentityRetentionDays, "UTC"));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<PlatformOverviewResponse>> OverviewAsync(CancellationToken ct)
    {
        var result = await platform.GetOverviewAsync(ct);
        await audit.RecordReadAsync("platform.overview", null, ct);
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<PagedResponse<PlatformAuditResponse>>> AuditAsync(
        [FromQuery] Guid? organizationId, [FromQuery] Guid? actorUserId, [FromQuery] string? actor,
        [FromQuery] string? action, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var filter = Filter(organizationId, actorUserId, action, from, to, page, pageSize, actor);
        var result = await platform.ListAuditAsync(filter, ct);
        await audit.RecordReadAsync("platform.audit", filter, ct, organizationId);
        return Ok(result);
    }

    [HttpPost("audit-logs/{id:guid}/details")]
    public async Task<ActionResult<PlatformAuditResponse>> DetailsAsync(Guid id, PlatformAuditDetailsRequest request, CancellationToken ct)
    {
        var entry = await platform.GetAuditDetailsAsync(id, request, ct);
        if (entry is null) return NotFound();
        await audit.RecordDetailsAsync(id, request.OrganizationId, ct);
        return Ok(entry);
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<PagedResponse<PlatformIncidentResponse>>> IncidentsAsync(
        [FromQuery] Guid? organizationId, [FromQuery] string? category,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var filter = Filter(organizationId, null, category, from, to, page, pageSize);
        var result = await platform.ListIncidentsAsync(filter, ct);
        await audit.RecordReadAsync("platform.incidents", filter, ct);
        return Ok(result);
    }

    private static PlatformLogFilter Filter(Guid? org, Guid? actorId, string? action, DateTimeOffset? from, DateTimeOffset? to, int page, int size, string? actor = null) =>
        new(org, actorId, string.IsNullOrWhiteSpace(action) ? null : action.Trim(), from ?? DateTimeOffset.UtcNow.AddDays(-7),
            to ?? DateTimeOffset.UtcNow, page, size, string.IsNullOrWhiteSpace(actor) ? null : actor.Trim());
}
