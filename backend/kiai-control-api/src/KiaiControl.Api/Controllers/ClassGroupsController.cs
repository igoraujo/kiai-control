using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.ClassGroups;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.ClassGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = PermissionCodes.ClassGroupsRead)]
[Route("api/v1/class-groups")]
public sealed class ClassGroupsController(OrganizationContext organizationContext, ClassGroupUseCase classGroupUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("class-groups", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<ActionResult<ClassGroupResponse>> CreateAsync(
        [FromBody] CreateClassGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await classGroupUseCase.CreateAsync(organizationId, request, cancellationToken);
            return created is null
                ? Conflict("The modality or branch is unavailable in this organization, or the class group name already exists.")
                : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<ClassGroupResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var classGroups = await classGroupUseCase.ListAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(classGroups);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<ClassGroupResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var classGroup = await classGroupUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        return classGroup is null ? NotFound() : Ok(classGroup);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<ActionResult<ClassGroupResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateClassGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await classGroupUseCase.UpdateAsync(organizationId, id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var archived = await classGroupUseCase.ArchiveAsync(organizationId, id, cancellationToken);
        return archived ? NoContent() : NotFound();
    }

    [HttpPost("{classGroupId:guid}/schedules")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<ActionResult<ClassScheduleResponse>> CreateScheduleAsync(
        Guid classGroupId,
        [FromBody] CreateClassScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await classGroupUseCase.CreateScheduleAsync(organizationId, classGroupId, request, cancellationToken);
            return created is null
                ? Conflict("The class group is unavailable or this schedule already exists.")
                : CreatedAtAction(nameof(GetById), new { id = classGroupId }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{classGroupId:guid}/schedules")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<ClassScheduleResponse>>> ListSchedulesAsync(
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (await classGroupUseCase.GetByIdAsync(organizationId, classGroupId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await classGroupUseCase.ListSchedulesAsync(organizationId, classGroupId, cancellationToken));
    }

    [HttpDelete("{classGroupId:guid}/schedules/{scheduleId:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<IActionResult> DeleteScheduleAsync(
        Guid classGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var deleted = await classGroupUseCase.DeleteScheduleAsync(organizationId, classGroupId, scheduleId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{classGroupId:guid}/enrollments")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<ActionResult<EnrollmentResponse>> CreateEnrollmentAsync(
        Guid classGroupId,
        [FromBody] CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await classGroupUseCase.CreateEnrollmentAsync(organizationId, classGroupId, request, cancellationToken);
            return created is null
                ? Conflict("The client is unavailable in this organization or the class group has no remaining capacity.")
                : CreatedAtAction(nameof(GetById), new { id = classGroupId }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{classGroupId:guid}/enrollments")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponse>>> ListEnrollmentsAsync(
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (await classGroupUseCase.GetByIdAsync(organizationId, classGroupId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await classGroupUseCase.ListEnrollmentsAsync(organizationId, classGroupId, cancellationToken));
    }

    [HttpDelete("{classGroupId:guid}/enrollments/{enrollmentId:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<IActionResult> CancelEnrollmentAsync(
        Guid classGroupId,
        Guid enrollmentId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var canceled = await classGroupUseCase.CancelEnrollmentAsync(organizationId, classGroupId, enrollmentId, cancellationToken);
        return canceled ? NoContent() : NotFound();
    }

    [HttpPut("{classGroupId:guid}/teachers/{teacherId:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<IActionResult> AssignTeacherAsync(
        Guid classGroupId,
        Guid teacherId,
        [FromBody] AssignClassGroupTeacherRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var assigned = await classGroupUseCase.AssignTeacherAsync(
                organizationId,
                classGroupId,
                teacherId,
                request.IsPrimary,
                request.StartsAt,
                request.EndsAt,
                cancellationToken);
            return assigned ? NoContent() : Conflict("The class group or active teacher is unavailable in this organization.");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{classGroupId:guid}/teachers")]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentResponse>>> ListTeacherAssignmentsAsync(
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (await classGroupUseCase.GetByIdAsync(organizationId, classGroupId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await classGroupUseCase.ListTeacherAssignmentsAsync(organizationId, classGroupId, cancellationToken));
    }

    [HttpDelete("{classGroupId:guid}/teachers/{teacherId:guid}")]
    [Authorize(Roles = "owner,admin")]
    [Authorize(Policy = PermissionCodes.ClassGroupsManage)]
    public async Task<IActionResult> RemoveTeacherAssignmentAsync(
        Guid classGroupId,
        Guid teacherId,
        [FromQuery] DateOnly startsAt,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var removed = await classGroupUseCase.RemoveTeacherAssignmentAsync(
            organizationId, classGroupId, teacherId, startsAt, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}