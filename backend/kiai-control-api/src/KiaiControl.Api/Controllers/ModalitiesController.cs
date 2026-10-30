using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin")]
[Authorize(Policy = PermissionCodes.ModalitiesManage)]
[Route("api/v1/modalities")]
public sealed class ModalitiesController(OrganizationContext organizationContext, ModalityUseCase modalityUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("modalities", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<ModalityResponse>> CreateAsync(
        [FromBody] CreateModalityRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await modalityUseCase.CreateAsync(organizationId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ModalityResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var modalities = await modalityUseCase.ListAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(modalities);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ModalityResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var modality = await modalityUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        return modality is null ? NotFound() : Ok(modality);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ModalityResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateModalityRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await modalityUseCase.UpdateAsync(organizationId, id, request, cancellationToken);
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
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var deactivated = await modalityUseCase.DeleteAsync(organizationId, id, cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }
}