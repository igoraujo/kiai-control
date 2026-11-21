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
[Route("api/v1/attendance")]
public sealed class AttendanceController(OrganizationContext organizationContext, AttendanceUseCase attendanceUseCase, KiaiControl.UseCases.Teachers.TeacherUseCase teachers, KiaiControl.UseCases.Lessons.LessonUseCase lessons) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("attendance", "ready", organizationContext.OrganizationId));
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpPost]
    public async Task<ActionResult<AttendanceRecordResponse>> CreateAsync([FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (request.OrganizationId != Guid.Empty && request.OrganizationId != organizationId)
        {
            return Forbid();
        }

        if (!await CanAccessLessonAsync(organizationId, request.LessonId, cancellationToken)) return NotFound();

        request.OrganizationId = organizationId;
        if (request.ClientId == Guid.Empty || request.LessonId == Guid.Empty)
        {
            return BadRequest(SystemMessages.ClientIdAndLessonIdAreRequired);
        }

        try
        {
            if (!TryGetPersonId(out var validatorPersonId))
            {
                return Unauthorized();
            }

            var created = await attendanceUseCase.CreateAsync(request, validatorPersonId, cancellationToken);
            return created is null ? Conflict(SystemMessages.AAulaOAlunoOuAMatriculaAtivaNaoEstaDisponivelNestaOrganizacao) : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "client")]
    [Authorize(Policy = PermissionCodes.AttendanceCheckIn)]
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceRecordResponse>> CheckInAsync([FromBody] CheckInRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var personId))
        {
            return Unauthorized();
        }

        try
        {
            var checkedInAt = DateTimeOffset.UtcNow;
            var record = await attendanceUseCase.CheckInAsync(organizationId, personId, request.LessonId, checkedInAt, cancellationToken);
            return record is null ? Conflict(SystemMessages.CheckInIndisponivelConfirmeSuaMatriculaOHorarioDaAulaOuSeJaRealizouOCheckIn) : StatusCode(StatusCodes.Status201Created, record);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "client")]
    [Authorize(Policy = PermissionCodes.AttendanceCheckIn)]
    [HttpDelete("check-in/{lessonId:guid}")]
    public async Task<IActionResult> CancelClientCheckInAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var personId))
        {
            return Unauthorized();
        }

        try
        {
            var cancelled = await attendanceUseCase.CancelClientCheckInAsync(organizationId, personId, lessonId, DateTimeOffset.UtcNow, cancellationToken);
            return cancelled ? NoContent() : Conflict(KiaiControl.Api.Common.SystemMessages.CancelamentoDoCheckInDisponivelSomenteAteOInicioDaAula);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ListAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var teacherId = await ScopedTeacherIdAsync(organizationContext.OrganizationId.Value, cancellationToken);
        if (teacherId == Guid.Empty) return NotFound();
        var attendance = await attendanceUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken, teacherId);
        return Ok(attendance);
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ListPendingAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        Guid? teacherId = null;
        if (User.IsInRole("teacher") && !User.IsInRole("owner") && !User.IsInRole("admin") && !User.IsInRole("reception"))
        {
            if (!Guid.TryParse(User.FindFirstValue("person_id"), out var person))
                return Unauthorized();
            var teacher = await teachers.GetByPersonIdAsync(organizationId, person, cancellationToken);
            if (teacher is null || teacher.Status != "active")
                return NotFound();
            teacherId = teacher.Id;
        }

        return Ok(await attendanceUseCase.ListPendingAsync(organizationId, page, pageSize, cancellationToken, teacherId));
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpPatch("{id:guid}/validate")]
    public async Task<ActionResult<AttendanceRecordResponse>> ValidateCheckInAsync(Guid id, [FromBody] ValidateCheckInRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var validatorPersonId))
        {
            return Unauthorized();
        }

        try
        {
            var pending = await attendanceUseCase.GetByIdAsync(organizationId, id, cancellationToken);
            if (pending is null || !await CanAccessLessonAsync(organizationId, pending.LessonId, cancellationToken)) return NotFound();
            var record = await attendanceUseCase.ValidateCheckInAsync(organizationId, id, validatorPersonId, request.Status, DateTimeOffset.UtcNow, cancellationToken);
            return record is null ? Conflict(SystemMessages.EsteCheckInJaFoiValidadoOuNaoEstaMaisDisponivel) : Ok(record);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttendanceRecordResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var attendance = await attendanceUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return attendance is null || !await CanAccessLessonAsync(organizationContext.OrganizationId.Value, attendance.LessonId, cancellationToken) ? NotFound() : Ok(attendance);
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AttendanceRecordResponse>> UpdateAsync(Guid id, [FromBody] UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            if (!TryGetPersonId(out var validatorPersonId))
            {
                return Unauthorized();
            }

            var original = await attendanceUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
            if (original is null || !await CanAccessLessonAsync(organizationContext.OrganizationId.Value, original.LessonId, cancellationToken) || !await CanAccessLessonAsync(organizationContext.OrganizationId.Value, request.LessonId, cancellationToken)) return NotFound();
            var updated = await attendanceUseCase.UpdateAsync(organizationContext.OrganizationId.Value, id, request, validatorPersonId, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var original = await attendanceUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        if (original is null || !await CanAccessLessonAsync(organizationContext.OrganizationId.Value, original.LessonId, cancellationToken)) return NotFound();
        var deleted = await attendanceUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<Guid?> ScopedTeacherIdAsync(Guid org, CancellationToken ct)
    {
        if (!User.IsInRole("teacher") || User.IsInRole("owner") || User.IsInRole("admin") || User.IsInRole("reception")) return null;
        if (!TryGetPersonId(out var person)) return Guid.Empty;
        var teacher = await teachers.GetByPersonIdAsync(org, person, ct);
        return teacher is { Status: "active" } ? teacher.Id : Guid.Empty;
    }

    private async Task<bool> CanAccessLessonAsync(Guid org, Guid lesson, CancellationToken ct)
    {
        var teacher = await ScopedTeacherIdAsync(org, ct);
        return teacher is null || teacher != Guid.Empty && await lessons.GetByIdForTeacherAsync(org, lesson, teacher.Value, ct) is not null;
    }

    private bool TryGetPersonId(out Guid personId) => Guid.TryParse(User.FindFirstValue("person_id"), out personId);
}
