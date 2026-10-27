using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,reception")]
[Route("api/v1/clients")]
public sealed class ClientsController(
    OrganizationContext organizationContext,
    ClientUseCase clientUseCase,
    ModalityUseCase modalityUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("clients", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateAsync([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
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

        try
        {
            var created = await clientUseCase.CreateAsync(request, cancellationToken);
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
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var clients = await clientUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var client = await clientUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);

        return client is null ? NotFound() : Ok(client);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> UpdateAsync(Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await clientUseCase.UpdateAsync(organizationContext.OrganizationId.Value, id, request, cancellationToken);
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
            return BadRequest("Organization context is required.");
        }

        var deleted = await clientUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{clientId:guid}/modalities/{modalityId:guid}")]
    public async Task<IActionResult> LinkModalityAsync(Guid clientId, Guid modalityId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var linked = await modalityUseCase.LinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
            return linked ? NoContent() : NotFound("The client or active modality is unavailable in this organization.");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{clientId:guid}/modalities/{modalityId:guid}")]
    public async Task<IActionResult> UnlinkModalityAsync(Guid clientId, Guid modalityId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var unlinked = await modalityUseCase.UnlinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
        return unlinked ? NoContent() : NotFound();
    }
}
