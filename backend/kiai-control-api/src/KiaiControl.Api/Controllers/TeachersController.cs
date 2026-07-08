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
    public async Task<ActionResult<IReadOnlyList<TeacherResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var teachers = await teacherUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var teachers = await teacherUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        var teacher = teachers.FirstOrDefault(item => item.Id == id);

        return teacher is null ? NotFound() : Ok(teacher);
    }
}
