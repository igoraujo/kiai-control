using KiaiControl.Contracts.Attendance;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Attendance;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = PermissionCodes.AttendanceManage)]
[Route("api/v1/attendance")]
public sealed class AttendanceController(OrganizationContext organizationContext, AttendanceUseCase attendanceUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("attendance", "ready", organizationContext.OrganizationId));
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [HttpPost]
    public async Task<ActionResult<AttendanceRecordResponse>> CreateAsync([FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (request.OrganizationId != Guid.Empty && request.OrganizationId != organizationId)
        {
            return Forbid();
        }

        request.OrganizationId = organizationId;

        if (request.ClientId == Guid.Empty || request.LessonId == Guid.Empty)
        {
            return BadRequest("ClientId and LessonId are required.");
        }

        try
        {
            if (!TryGetPersonId(out var validatorPersonId))
            {
                return Unauthorized();
            }

            var created = await attendanceUseCase.CreateAsync(request, validatorPersonId, cancellationToken);
            return created is null
                ? Conflict("A aula, o aluno ou a matrícula ativa não está disponível nesta organização.")
                : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "client")]
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceRecordResponse>> CheckInAsync(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var personId))
        {
            return Unauthorized();
        }

        try
        {
            var checkedInAt = DateTimeOffset.UtcNow;
            var record = await attendanceUseCase.CheckInAsync(
                organizationId, personId, request.LessonId, checkedInAt, cancellationToken);
            return record is null
                ? Conflict("Check-in indisponível: confirme sua matrícula, o horário da aula ou se já realizou o check-in.")
                : StatusCode(StatusCodes.Status201Created, record);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
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

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ListPendingAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await attendanceUseCase.ListPendingAsync(organizationId, page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [HttpPatch("{id:guid}/validate")]
    public async Task<ActionResult<AttendanceRecordResponse>> ValidateCheckInAsync(
        Guid id,
        [FromBody] ValidateCheckInRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var validatorPersonId))
        {
            return Unauthorized();
        }

        try
        {
            var record = await attendanceUseCase.ValidateCheckInAsync(
                organizationId, id, validatorPersonId, request.Status, DateTimeOffset.UtcNow, cancellationToken);
            return record is null ? Conflict("Este check-in já foi validado ou não está mais disponível.") : Ok(record);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
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

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AttendanceRecordResponse>> UpdateAsync(Guid id, [FromBody] UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            if (!TryGetPersonId(out var validatorPersonId))
            {
                return Unauthorized();
            }

            var updated = await attendanceUseCase.UpdateAsync(
                organizationContext.OrganizationId.Value, id, request, validatorPersonId, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
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

    private bool TryGetPersonId(out Guid personId) =>
        Guid.TryParse(User.FindFirstValue("person_id"), out personId);
}
