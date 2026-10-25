using KiaiControl.Contracts.Organizations;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin")]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(
    OrganizationContext organizationContext,
    OrganizationUseCase organizationUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<OrganizationRegistrationResponse>> RegisterAsync(
        [FromBody] RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var registration = await organizationUseCase.RegisterAsync(request, cancellationToken);
            return registration is null
                ? Conflict("The organization document or owner e-mail is already registered.")
                : CreatedAtAction(nameof(GetById), new { id = registration.Organization.Id }, registration);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (id != organizationId)
        {
            return Forbid();
        }

        var organization = await organizationUseCase.GetByIdAsync(organizationId, cancellationToken);
        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        if (id != organizationId)
        {
            return Forbid();
        }

        try
        {
            var organization = await organizationUseCase.UpdateAsync(organizationId, request, cancellationToken);
            return organization is null ? NotFound() : Ok(organization);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}