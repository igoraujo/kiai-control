using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Branches;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,reception")]
[Authorize(Policy = PermissionCodes.BranchesManage)]
[Route("api/v1/branches")]
public sealed class BranchesController(OrganizationContext organizationContext, BranchUseCase branchUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BranchResponse>> CreateAsync(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var branch = await branchUseCase.CreateAsync(organizationId, request, cancellationToken);
            return branch is null
                ? Conflict(SystemMessages.ABranchWithThisNameAlreadyExistsInTheOrganization)
                : CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BranchResponse>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var branches = await branchUseCase.ListAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(branches);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BranchResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var branch = await branchUseCase.GetByIdAsync(organizationId, id, cancellationToken);
        return branch is null ? NotFound() : Ok(branch);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BranchResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var branch = await branchUseCase.UpdateAsync(organizationId, id, request, cancellationToken);
            return branch is null ? NotFound() : Ok(branch);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        if (await branchUseCase.GetByIdAsync(organizationId, id, cancellationToken) is null)
        {
            return NotFound();
        }

        var deleted = await branchUseCase.DeleteAsync(organizationId, id, cancellationToken);
        return deleted ? NoContent() : Conflict(SystemMessages.TheBranchIsReferencedByPeopleOrClassGroups);
    }
}