using System.Security.Claims;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Billing;
using KiaiControl.UseCases.Fitness;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController, Authorize(Roles = "client"), Route("api/v1/portal")]
public sealed class StudentPortalController(OrganizationContext context, ClientUseCase clients, ClientJourneyUseCase journey, BillingUseCase billing, FitnessUseCase fitness) : ControllerBase
{
    [HttpGet("journey")]
    public Task<IActionResult> Journey(int page = 1, int pageSize = 20, CancellationToken ct = default) => ForClient(async (org, id) => Ok(await journey.GetAsync(org, id, page, pageSize, ct)), ct);
    [HttpGet("invoices")]
    public Task<IActionResult> Invoices(int page = 1, int pageSize = 20, CancellationToken ct = default) => ForClient(async (org, id) =>
    {
        KiaiControl.UseCases.Catalog.CatalogUseCase.Validate(org, page, pageSize, null);
        return Ok(await billing.ListAsync(org, page, pageSize, ct, id));
    }, ct);
    [HttpGet("payments")]
    public Task<IActionResult> Payments(int page = 1, int pageSize = 20, CancellationToken ct = default) => ForClient(async (org, id) =>
    {
        KiaiControl.UseCases.Catalog.CatalogUseCase.Validate(org, page, pageSize, null);
        return Ok(await billing.ListPaymentsAsync(org, page, pageSize, ct, id));
    }, ct);
    [HttpGet("goals")]
    public Task<IActionResult> Goals(int page = 1, int pageSize = 20, CancellationToken ct = default) => ForClient(async (org, id) =>
    {
        KiaiControl.UseCases.Catalog.CatalogUseCase.Validate(org, page, pageSize, null);
        return Ok(await fitness.ListAsync(org, id, page, pageSize, ct));
    }, ct);
    [HttpGet("goals/{goalId:guid}/progress")]
    public Task<IActionResult> GoalProgress(Guid goalId, int page = 1, int pageSize = 20, CancellationToken ct = default) => ForClient(async (org, id) =>
    {
        KiaiControl.UseCases.Catalog.CatalogUseCase.Validate(org, page, pageSize, null);
        // Ownership is checked in the repository, independently of any supplied client identifier.
        var goals = await journey.GetGoalClientIdAsync(org, goalId, ct);
        return goals != id ? NotFound() : Ok(await fitness.ListProgressAsync(org, goalId, page, pageSize, ct));
    }, ct);
    private async Task<IActionResult> ForClient(Func<Guid, Guid, Task<IActionResult>> action, CancellationToken ct)
    {
        if (context.OrganizationId is not Guid org || !Guid.TryParse(User.FindFirstValue("person_id"), out var person))
            return Unauthorized();
        var client = await clients.GetByPersonIdAsync(org, person, ct);
        if (client is null || client.Status != "active")
            return NotFound();
        try
        {
            return await action(org, client.Id);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}
