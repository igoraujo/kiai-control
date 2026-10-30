using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Attendance;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Attendance;
using KiaiControl.UseCases.Lessons;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = PermissionCodes.LessonsManage)]
[Route("api/v1/lessons")]
public sealed class LessonsController(
    OrganizationContext organizationContext,
    LessonUseCase lessonUseCase,
    AttendanceUseCase attendanceUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("lessons", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<LessonResponse>> CreateAsync(
        [FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await lessonUseCase.CreateAsync(organizationId, request, cancellationToken);
            return created is null
                ? Conflict("The class group is unavailable in this organization.")
                : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("generate")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> GenerateAsync(
        [FromBody] GenerateLessonsRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await lessonUseCase.GenerateFromSchedulesAsync(organizationId, request, cancellationToken);
            return created is null
                ? NotFound("The class group is unavailable in this organization.")
                : CreatedAtAction(nameof(ListAsync), new { page = 1, pageSize = Math.Max(created.Count, 1) }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var lessons = await lessonUseCase.ListAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(lessons);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<LessonResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var lesson = await lessonUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        return lesson is null ? NotFound() : Ok(lesson);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<LessonResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await lessonUseCase.UpdateAsync(organizationId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<LessonResponse>> CancelAsync(
        Guid id,
        [FromBody] CancelLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var canceled = await lessonUseCase.CancelAsync(organizationId, id, request, cancellationToken);
            return canceled is null ? NotFound() : Ok(canceled);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPatch("{id:guid}/reschedule")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<LessonResponse>> RescheduleAsync(
        Guid id,
        [FromBody] RescheduleLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var rescheduled = await lessonUseCase.RescheduleAsync(organizationId, id, request, cancellationToken);
            return rescheduled is null ? NotFound() : Ok(rescheduled);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{id:guid}/attendances")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> GetAttendancesAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var lesson = await lessonUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        if (lesson is null)
        {
            return NotFound();
        }

        var attendance = await attendanceUseCase.ListByLessonAsync(organizationId, id, cancellationToken);
        return Ok(attendance);
    }

    [HttpPut("{id:guid}/attendances")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> SaveAttendancesAsync(
        Guid id,
        [FromBody] SaveAttendanceCallRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (await lessonUseCase.GetByIdAsync(organizationId, id, cancellationToken) is null)
        {
            return NotFound();
        }

        if (!Guid.TryParse(User.FindFirstValue("person_id"), out var validatorPersonId))
        {
            return Unauthorized();
        }

        try
        {
            var attendance = await attendanceUseCase.SaveLessonCallAsync(
                organizationId, id, validatorPersonId, request, cancellationToken);
            return attendance is null
                ? Conflict("The lesson is unavailable or one or more clients are not actively enrolled in its class group.")
                : Ok(attendance);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}