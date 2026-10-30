using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin")]
[Authorize(Policy = PermissionCodes.AccessManage)]
[Route("api/v1/access")]
public sealed class AccessControlController(
    OrganizationContext organizationContext,
    AccessControlUseCase accessControlUseCase) : ControllerBase
{
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> ListRolesAsync(CancellationToken cancellationToken)
    {
        return Ok(await accessControlUseCase.ListRolesAsync(cancellationToken));
    }

    [HttpGet("permissions")]
    [Authorize(Roles = "owner")]
    [Authorize(Policy = PermissionCodes.AccessPermissionsManage)]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await accessControlUseCase.ListPermissionsAsync(cancellationToken));
    }

    [HttpGet("people/{personId:guid}/roles")]
    public async Task<ActionResult<IReadOnlyList<string>>> ListPersonRolesAsync(Guid personId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await accessControlUseCase.ListPersonRolesAsync(organizationId, personId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpPut("people/{personId:guid}/roles")]
    public async Task<IActionResult> AssignRoleAsync(
        Guid personId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var assigned = await accessControlUseCase.AssignPersonRoleAsync(
                organizationId,
                personId,
                actorUserId,
                request.RoleCode,
                User.IsInRole("owner"),
                cancellationToken);
            return assigned ? NoContent() : NotFound("Person or role was not found in this organization.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("people/{personId:guid}/roles/{roleCode}")]
    public async Task<IActionResult> RevokeRoleAsync(Guid personId, string roleCode, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var revoked = await accessControlUseCase.RevokePersonRoleAsync(
                organizationId,
                personId,
                roleCode,
                User.IsInRole("owner"),
                cancellationToken);
            return revoked ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("roles/{roleCode}/permissions")]
    [Authorize(Roles = "owner")]
    [Authorize(Policy = PermissionCodes.AccessPermissionsManage)]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> ListRolePermissionsAsync(string roleCode, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accessControlUseCase.ListRolePermissionsAsync(roleCode, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpPut("roles/{roleCode}/permissions")]
    [Authorize(Roles = "owner")]
    [Authorize(Policy = PermissionCodes.AccessPermissionsManage)]
    public async Task<IActionResult> GrantPermissionAsync(
        string roleCode,
        [FromBody] SetRolePermissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var granted = await accessControlUseCase.GrantRolePermissionAsync(roleCode, request.PermissionCode, cancellationToken);
            return granted ? NoContent() : NotFound("Role or permission code was not found.");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("roles/{roleCode}/permissions/{permissionCode}")]
    [Authorize(Roles = "owner")]
    [Authorize(Policy = PermissionCodes.AccessPermissionsManage)]
    public async Task<IActionResult> RevokePermissionAsync(string roleCode, string permissionCode, CancellationToken cancellationToken)
    {
        try
        {
            var revoked = await accessControlUseCase.RevokeRolePermissionAsync(roleCode, permissionCode, cancellationToken);
            return revoked ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}