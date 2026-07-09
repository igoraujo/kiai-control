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

        if (request.OrganizationId == Guid.Empty || request.ClientId == Guid.Empty || request.LessonId == Guid.Empty)
        {
            return BadRequest("OrganizationId, ClientId and LessonId are required.");
        }

        try
        {
            var created = await attendanceUseCase.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var attendance = await attendanceUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(attendance);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttendanceRecordResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var attendance = await attendanceUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return attendance is null ? NotFound() : Ok(attendance);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AttendanceRecordResponse>> UpdateAsync(Guid id, [FromBody] UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await attendanceUseCase.UpdateAsync(organizationContext.OrganizationId.Value, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var deleted = await attendanceUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
