using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Students;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/students")]
public sealed class StudentsController(OrganizationContext organizationContext, StudentUseCase studentUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("students", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<StudentResponse>> CreateAsync([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        request.OrganizationId = request.OrganizationId == Guid.Empty ? organizationContext.OrganizationId ?? Guid.Empty : request.OrganizationId;

        if (request.OrganizationId == Guid.Empty)
        {
            return BadRequest("OrganizationId is required.");
        }

        var created = await studentUseCase.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var students = await studentUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var students = await studentUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        var student = students.FirstOrDefault(item => item.Id == id);

        return student is null ? NotFound() : Ok(student);
    }
}
