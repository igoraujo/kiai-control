using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize(Roles = "owner,admin")]
[Authorize(Policy = PermissionCodes.OrganizationsManage)]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(OrganizationContext organizationContext, OrganizationUseCase organizationUseCase, KiaiControl.UseCases.Administration.AdministrationUseCase administration) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        if (organizationContext.OrganizationId is not Guid org)
            return Unauthorized();
        return Ok(await administration.GetSettingsAsync(org, ct));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings(KiaiControl.Contracts.Administration.OrganizationSettingsResponse request, CancellationToken ct)
    {
        if (organizationContext.OrganizationId is not Guid org)
            return Unauthorized();
        try
        {
            await administration.SaveSettingsAsync(org, request, ct);
            return NoContent();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (id != organizationId)
        {
            return Forbid();
        }

        var organization = await organizationUseCase.GetByIdAsync(organizationId, cancellationToken);
        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationResponse>> UpdateAsync(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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
