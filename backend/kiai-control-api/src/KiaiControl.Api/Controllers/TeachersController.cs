using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Lessons;
using KiaiControl.UseCases.Modalities;
using KiaiControl.UseCases.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin")]
[Authorize(Policy = PermissionCodes.TeachersManage)]
[Route("api/v1/teachers")]
public sealed class TeachersController(
    OrganizationContext organizationContext,
    TeacherUseCase teacherUseCase,
    ModalityScheduleUseCase modalityScheduleUseCase,
    LessonUseCase lessonUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("teachers", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<TeacherResponse>> CreateAsync([FromBody] CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (request.OrganizationId != Guid.Empty && request.OrganizationId != organizationId)
        {
            return Forbid();
        }

        request.OrganizationId = organizationId;

        try
        {
            var created = await teacherUseCase.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ConflictException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeacherResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var teachers = await teacherUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var teacher = await teacherUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);

        return teacher is null ? NotFound() : Ok(teacher);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeacherResponse>> UpdateAsync(Guid id, [FromBody] UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var updated = await teacherUseCase.UpdateAsync(organizationContext.OrganizationId.Value, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ConflictException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var deleted = await teacherUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/schedules")]
    public async Task<ActionResult<IReadOnlyList<ModalityScheduleResponse>>> ListSchedulesAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (await teacherUseCase.GetByIdAsync(organizationId, id, cancellationToken) is null)
        {
            return NotFound();
        }

        var schedules = await modalityScheduleUseCase.ListByTeacherAsync(organizationId, id, cancellationToken);
        return Ok(schedules);
    }

    [HttpPost("{id:guid}/schedules")]
    public async Task<ActionResult<ModalityScheduleResponse>> CreateScheduleAsync(
        Guid id,
        [FromBody] CreateTeacherScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var teacher = await teacherUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        if (!string.Equals(teacher.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(SystemMessages.OnlyActiveTeachersCanBeAssignedToASchedule);
        }

        try
        {
            var schedule = await modalityScheduleUseCase.CreateAsync(organizationId, request.ModalityId, new()
            {
                BranchId = request.BranchId,
                TeacherId = id,
                DayOfWeek = request.DayOfWeek,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt
            }, cancellationToken);
            return schedule is null
                ? Conflict(SystemMessages.TheModalityOrBranchIsUnavailableOrThisScheduleAlreadyExists)
                : CreatedAtAction(nameof(ListSchedulesAsync), new { id }, schedule);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}/schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteScheduleAsync(
        Guid id,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var schedules = await modalityScheduleUseCase.ListByTeacherAsync(organizationId, id, cancellationToken);
        var schedule = schedules.FirstOrDefault(item => item.Id == scheduleId);
        if (schedule is null)
        {
            return NotFound();
        }

        var deleted = await modalityScheduleUseCase.DeactivateAsync(
            organizationId, schedule.ModalityId, scheduleId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/lessons")]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> ListLessonsAsync(
        Guid id,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (await teacherUseCase.GetByIdAsync(organizationId, id, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            var lessons = await lessonUseCase.ListAsync(
                organizationId, page, pageSize, cancellationToken, id, from, to);
            return Ok(lessons);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
