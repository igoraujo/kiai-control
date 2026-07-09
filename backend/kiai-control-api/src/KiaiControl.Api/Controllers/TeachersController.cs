using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/teachers")]
public sealed class TeachersController(OrganizationContext organizationContext, TeacherUseCase teacherUseCase) : ControllerBase
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
        request.OrganizationId = request.OrganizationId == Guid.Empty ? organizationContext.OrganizationId ?? Guid.Empty : request.OrganizationId;

        if (request.OrganizationId == Guid.Empty)
        {
            return BadRequest("OrganizationId is required.");
        }

        var created = await teacherUseCase.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeacherResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var teachers = await teacherUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var teacher = await teacherUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);

        return teacher is null ? NotFound() : Ok(teacher);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeacherResponse>> UpdateAsync(Guid id, [FromBody] UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
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
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var deleted = await teacherUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
