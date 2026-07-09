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

        if (request.OrganizationId == Guid.Empty || request.ClientId == Guid.Empty)
        {
            return BadRequest("OrganizationId and ClientId are required.");
        }

        var created = await billingUseCase.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListAsync), new { }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BillingResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest("Organization context is required.");
        }

        var billingItems = await billingUseCase.ListAsync(organizationContext.OrganizationId.Value, cancellationToken);
        return Ok(billingItems);
    }
}
