using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Reports;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,financial")]
[Authorize(Policy = PermissionCodes.ReportsRead)]
[Route("api/v1/reports")]
public sealed class ReportsController(OrganizationContext organizationContext, ReportUseCase reportUseCase) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardReportResponse>> DashboardAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await reportUseCase.DashboardAsync(organizationId, from, to, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<IReadOnlyList<AttendanceReportRow>>> AttendanceAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? modalityId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await reportUseCase.AttendanceAsync(organizationId, from, to, modalityId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("financial")]
    public async Task<ActionResult<FinancialReportResponse>> FinancialAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await reportUseCase.FinancialAsync(organizationId, from, to, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("client-performance")]
    public async Task<ActionResult<IReadOnlyList<ClientPerformanceReportRow>>> ClientPerformanceAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? modalityId,
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
            return Ok(await reportUseCase.ClientPerformanceAsync(organizationId, from, to, modalityId, page, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("graduation")]
    public async Task<ActionResult<IReadOnlyList<GraduationReportRow>>> GraduationAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? modalityId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await reportUseCase.GraduationAsync(organizationId, from, to, modalityId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}