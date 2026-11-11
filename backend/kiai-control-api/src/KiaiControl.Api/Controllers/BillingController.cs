using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Billing;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize(Roles = "owner,admin,financial")]
[Authorize(Policy = PermissionCodes.BillingManage)]
[Route("api/v1/billing")]
public sealed class BillingController(OrganizationContext organizationContext, BillingUseCase billingUseCase, BillingCatalogUseCase billingCatalogUseCase, KiaiControl.UseCases.Clients.ClientUseCase clients) : ControllerBase
{
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        if (organizationContext.OrganizationId is not Guid org)
            return Unauthorized();
        return Ok(new BillingOptionsResponse { Clients = await clients.ListAllOptionsAsync(org, ct), Plans = await KiaiControl.UseCases.Common.PagedOptions.ReadAllAsync(page => billingCatalogUseCase.ListPlansAsync(org, page, 200, ct)) });
    }

    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("billing", "ready", organizationContext.OrganizationId));
    }

    [HttpPost]
    public async Task<ActionResult<BillingResponse>> CreateAsync([FromBody] CreateBillingRequest request, CancellationToken cancellationToken)
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
        if (request.ClientId == Guid.Empty || request.ClientPlanSubscriptionId == Guid.Empty)
        {
            return BadRequest(SystemMessages.ClientIdAndClientPlanSubscriptionIdAreRequired);
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
    public async Task<ActionResult<IReadOnlyList<BillingResponse>>> ListAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? clientId = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (clientId == Guid.Empty)
        {
            return BadRequest(SystemMessages.ClientIdMustBeAValidIdentifier);
        }

        if (search?.Length > 200)
            return BadRequest("Use até 200 caracteres na busca.");
        var billingItems = await billingUseCase.ListAsync(organizationContext.OrganizationId.Value, page, pageSize, cancellationToken, clientId, search);
        return Ok(billingItems);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var billing = await billingUseCase.GetByIdAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return billing is null ? NotFound() : Ok(billing);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BillingResponse>> UpdateAsync(Guid id, [FromBody] UpdateBillingRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is null)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var deleted = await billingUseCase.DeleteAsync(organizationContext.OrganizationId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentResponse>> RegisterPaymentAsync(Guid id, [FromBody] RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var payment = await billingUseCase.RegisterPaymentAsync(organizationId, id, request, cancellationToken);
            return payment is null ? Conflict(SystemMessages.TheInvoiceIsUnavailableClosedOrThePaymentExceedsItsOutstandingBalance) : CreatedAtAction(nameof(GetById), new { id }, payment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> ListPaymentsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? clientId = null, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (clientId == Guid.Empty)
        {
            return BadRequest(SystemMessages.ClientIdMustBeAValidIdentifier);
        }

        var payments = await billingUseCase.ListPaymentsAsync(organizationId, page, pageSize, cancellationToken, clientId);
        return Ok(payments);
    }

    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> ListPaymentsByInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (await billingUseCase.GetByIdAsync(organizationId, id, cancellationToken)is null)
        {
            return NotFound();
        }

        var payments = await billingUseCase.ListPaymentsByInvoiceAsync(organizationId, id, cancellationToken);
        return Ok(payments);
    }

    [HttpPost("plans")]
    public async Task<ActionResult<BillingPlanResponse>> CreatePlanAsync([FromBody] CreateBillingPlanRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var plan = await billingCatalogUseCase.CreatePlanAsync(organizationId, request, cancellationToken);
            return CreatedAtAction("ListPlans", new { page = 1, pageSize = 50 }, plan);
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

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<BillingPlanResponse>>> ListPlansAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var plans = await billingCatalogUseCase.ListPlansAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(plans);
    }

    [HttpPut("plans/{id:guid}")]
    public async Task<ActionResult<BillingPlanResponse>> UpdatePlanAsync(Guid id, [FromBody] UpdateBillingPlanRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var plan = await billingCatalogUseCase.UpdatePlanAsync(organizationId, id, request, cancellationToken);
            return plan is null ? NotFound() : Ok(plan);
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

    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeactivatePlanAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var deactivated = await billingCatalogUseCase.DeactivatePlanAsync(organizationId, id, cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    [HttpPost("clients/{clientId:guid}/subscriptions")]
    public async Task<ActionResult<ClientPlanSubscriptionResponse>> CreateSubscriptionAsync(Guid clientId, [FromBody] CreateClientSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var subscription = await billingCatalogUseCase.CreateSubscriptionAsync(organizationId, clientId, request, cancellationToken);
            return subscription is null ? Conflict(SystemMessages.TheClientOrActivePlanIsUnavailableOrTheClientAlreadyHasAnActiveSubscription) : StatusCode(StatusCodes.Status201Created, subscription);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult<IReadOnlyList<ClientPlanSubscriptionResponse>>> ListSubscriptionsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? clientId = null, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (clientId == Guid.Empty)
        {
            return BadRequest(SystemMessages.ClientIdMustBeAValidIdentifier);
        }

        return Ok(await billingCatalogUseCase.ListSubscriptionsAsync(organizationId, page, pageSize, cancellationToken, clientId));
    }

    [HttpPatch("subscriptions/{subscriptionId:guid}/cancel")]
    public async Task<ActionResult<ClientPlanSubscriptionResponse>> CancelSubscriptionAsync(Guid subscriptionId, [FromBody] CancelClientSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var subscription = await billingCatalogUseCase.CancelSubscriptionAsync(organizationId, subscriptionId, request, cancellationToken);
            return subscription is null ? NotFound() : Ok(subscription);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("invoices/generate")]
    public async Task<ActionResult<IReadOnlyList<BillingResponse>>> GenerateInvoicesAsync([FromBody] GenerateInvoicesRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var invoices = await billingCatalogUseCase.GenerateInvoicesAsync(organizationId, request, cancellationToken);
            return Ok(invoices);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
