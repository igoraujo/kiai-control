using KiaiControl.Contracts.Attendance;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
public sealed class AttendanceController(OrganizationContext organizationContext, AttendanceUseCase attendanceUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("attendance", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<AttendanceRecordResponse>> CreateAsync([FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        request.OrganizationId = request.OrganizationId == Guid.Empty ? organizationContext.OrganizationId ?? Guid.Empty : request.OrganizationId;

        if (request.OrganizationId == Guid.Empty || request.StudentId == Guid.Empty)
        {
            return BadRequest("OrganizationId and StudentId are required.");
        }

        var created = await attendanceUseCase.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListAsync), new { }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var attendance = await attendanceUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        return Ok(attendance);
    }
}
