using KiaiControl.Contracts.Billing;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class BillingController(OrganizationContext organizationContext, BillingUseCase billingUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("billing", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<BillingResponse>> CreateAsync([FromBody] CreateBillingRequest request, CancellationToken cancellationToken)
    {
        request.OrganizationId = request.OrganizationId == Guid.Empty ? organizationContext.OrganizationId ?? Guid.Empty : request.OrganizationId;

        if (request.OrganizationId == Guid.Empty || request.ClientId == Guid.Empty || request.ClientPlanSubscriptionId == Guid.Empty)
        {
            return BadRequest("OrganizationId, ClientId and ClientPlanSubscriptionId are required.");
        }

        try
        {
            var created = await billingUseCase.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BillingResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var billingItems = await billingUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken);
        return Ok(billingItems);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var billing = await billingUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return billing is null ? NotFound() : Ok(billing);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BillingResponse>> UpdateAsync(Guid id, [FromBody] UpdateBillingRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var updated = await billingUseCase.UpdateAsync(organizationContext.OrganizationId.Value, id, request, cancellationToken);
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

        var deleted = await billingUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
