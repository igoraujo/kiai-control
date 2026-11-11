using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Common;
using KiaiControl.Contracts.Modalities;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize(Roles = "owner,admin,reception")]
[Authorize(Policy = PermissionCodes.ClientsManage)]
[Route("api/v1/clients")]
public sealed class ClientsController(OrganizationContext organizationContext, ClientUseCase clientUseCase, ClientJourneyUseCase journeyUseCase, ModalityUseCase modalityUseCase, KiaiControl.UseCases.Administration.AdministrationUseCase administration) : ControllerBase
{
    [HttpGet("people")]
    public async Task<IActionResult> ExistingPeople(CancellationToken ct)
    {
        if(organizationContext.OrganizationId is not Guid org)return Unauthorized();
        return Ok(await administration.ProfileCandidatesAsync(org,"client",User.IsInRole("owner"),ct));
    }
    [HttpPost("people/{personId:guid}/profile")]
    public async Task<IActionResult> LinkProfile(Guid personId,CancellationToken ct)
    {
        if(organizationContext.OrganizationId is not Guid org || !Guid.TryParse(User.FindFirst("sub")?.Value,out var actor))return Unauthorized();
        if(!await administration.AddProfileAsync(org,personId,"client",actor,ct))return NotFound();
        return Ok(await clientUseCase.GetByPersonIdAsync(org,personId,ct));
    }

    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("clients", "ready", organizationContext.OrganizationId));
    }

    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> Progress(Guid id, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (organizationContext.OrganizationId is not Guid org)
            return Unauthorized();
        try
        {
            var result = await journeyUseCase.GetAsync(org, id, page, pageSize, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateAsync([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
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
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> ListAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var clients = await clientUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var client = await clientUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpGet("modalities")]
    public async Task<ActionResult<IReadOnlyList<ModalityResponse>>> ListActiveModalitiesAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var modalities = await modalityUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        return Ok(modalities.Where(modality => modality.IsActive).ToList());
    }

    [HttpGet("{clientId:guid}/modalities")]
    public async Task<ActionResult<IReadOnlyList<ModalityResponse>>> ListClientModalitiesAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (await clientUseCase.GetByIdAsync(organizationId, clientId, cancellationToken)is null)
        {
            return NotFound();
        }

        return Ok(await modalityUseCase.ListClientModalitiesAsync(organizationId, clientId, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> UpdateAsync(Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var deleted = await clientUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{clientId:guid}/modalities/{modalityId:guid}")]
    public async Task<IActionResult> LinkModalityAsync(Guid clientId, Guid modalityId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var linked = await modalityUseCase.LinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
            return linked ? NoContent() : NotFound(SystemMessages.TheClientOrActiveModalityIsUnavailableInThisOrganization);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var unlinked = await modalityUseCase.UnlinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
        return unlinked ? NoContent() : NotFound();
    }
}
