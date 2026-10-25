using KiaiControl.Contracts.Audit;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin")]
[Route("api/v1/audit-logs")]
public sealed class AuditController(OrganizationContext organizationContext, AuditLogUseCase auditLogUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> ListAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var logs = await auditLogUseCase.ListAsync(organizationId, from, to, page, pageSize, cancellationToken);
            return Ok(logs);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}