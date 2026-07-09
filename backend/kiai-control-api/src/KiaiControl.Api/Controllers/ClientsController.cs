using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public sealed class ClientsController(OrganizationContext organizationContext, ClientUseCase clientUseCase) : ControllerBase
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
        request.OrganizationId = request.OrganizationId == Guid.Empty ? organizationContext.OrganizationId ?? Guid.Empty : request.OrganizationId;

        if (request.OrganizationId == Guid.Empty)
        {
            return BadRequest("OrganizationId is required.");
        }

        var created = await clientUseCase.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
}
