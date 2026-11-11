using KiaiControl.Contracts.Organizations;
using KiaiControl.UseCases.Organizations;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Platform;
using KiaiControl.UseCases.Platform;
using KiaiControl.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "platform_owner")]
[Route("api/v1/platform/organizations")]
[TypeFilter(typeof(PlatformExceptionFilter))]
public sealed class PlatformOrganizationsController(
    OrganizationUseCase organizationUseCase,
    PlatformAdministrationUseCase platform,
    PlatformAccessAudit audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrganizationResponse>>> ListAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string archive = "active", CancellationToken cancellationToken = default)
    {
        var result = await platform.ListOrganizationsAsync(page, pageSize, search, status, cancellationToken, archive);
        await audit.RecordReadAsync("platform.organizations", new { page, pageSize, search, status }, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationRegistrationResponse>> RegisterAsync(
        [FromBody] RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var registration = await organizationUseCase.RegisterAsync(request, cancellationToken,
                audit.Create("platform.organization.created", "platform.organizations"));
            return registration is null
                ? Conflict(SystemMessages.TheOrganizationDocumentOrOwnerEMailIsAlreadyRegistered)
                : StatusCode(StatusCodes.Status201Created, registration);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<ActionResult<OrganizationResponse>> GetAsync(Guid organizationId, CancellationToken ct)
    {
        if (organizationId == KiaiControl.Core.Common.PlatformIdentity.OrganizationId) return NotFound();
        var organization = await platform.GetOrganizationAsync(organizationId, ct);
        if (organization is null) return NotFound();
        await audit.RecordReadAsync("platform.organizations", new { organizationId }, ct, organizationId);
        return Ok(organization);
    }

    [HttpDelete("{organizationId:guid}")]
    public async Task<IActionResult> ArchiveAsync(Guid organizationId, [FromBody] PlatformArchiveRequest request, CancellationToken ct) =>
        await platform.ChangeArchiveAsync(organizationId, true, request,
            audit.Create("platform.organization.archived", "platform.organizations", organizationId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPost("{organizationId:guid}/restore")]
    public async Task<IActionResult> RestoreAsync(Guid organizationId, PlatformArchiveRequest request, CancellationToken ct) =>
        await platform.ChangeArchiveAsync(organizationId, false, request,
            audit.Create(request.Reactivate ? "platform.organization.reactivated" : "platform.organization.restored",
                "platform.organizations", organizationId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPut("{organizationId:guid}/privacy-hold")]
    public async Task<IActionResult> PrivacyHoldAsync(Guid organizationId, PlatformPrivacyHoldRequest request, CancellationToken ct) =>
        await platform.SetPrivacyHoldAsync(organizationId, request,
            audit.Create("platform.organization.privacy_hold_changed", "platform.organizations", organizationId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPut("{organizationId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid organizationId, PlatformOrganizationUpdateRequest request, CancellationToken ct) =>
        await platform.UpdateOrganizationAsync(organizationId, request,
            audit.Create("platform.organization.updated", "platform.organizations", organizationId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPut("{organizationId:guid}/status")]
    public async Task<IActionResult> StatusAsync(Guid organizationId, OrganizationStatusRequest request, CancellationToken ct) =>
        await platform.SetOrganizationStatusAsync(organizationId, request,
            audit.Create("platform.organization.status_changed", "platform.organizations", organizationId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpGet("{organizationId:guid}/accounts")]
    public async Task<ActionResult<PagedResponse<PlatformAccountResponse>>> AccountsAsync(Guid organizationId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await platform.ListAccountsAsync(organizationId, page, pageSize, search, ct);
        await audit.RecordReadAsync("platform.accounts", new { organizationId, page, pageSize, search }, ct);
        return Ok(result);
    }

    [HttpPost("{organizationId:guid}/accounts")]
    public async Task<IActionResult> CreateAccountAsync(Guid organizationId, CreatePlatformAccountRequest request, CancellationToken ct) =>
        await platform.CreateAccountAsync(organizationId, request,
            audit.Create("platform.account.created", "platform.accounts", reason: request.Reason), ct) ? StatusCode(201) : NotFound();

    [HttpPut("{organizationId:guid}/accounts/{userId:guid}")]
    public async Task<IActionResult> UpdateAccountAsync(Guid organizationId, Guid userId, UpdatePlatformAccountRequest request, CancellationToken ct) =>
        await platform.UpdateAccountAsync(organizationId, userId, request,
            audit.Create("platform.account.updated", "platform.accounts", userId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPut("{organizationId:guid}/accounts/{userId:guid}/password")]
    public async Task<IActionResult> ResetPasswordAsync(Guid organizationId, Guid userId, PlatformPasswordRequest request, CancellationToken ct) =>
        await platform.ResetPasswordAsync(organizationId, userId, request,
            audit.Create("platform.account.password_reset", "platform.accounts", userId, request.Reason), ct) ? NoContent() : NotFound();

    [HttpPost("{organizationId:guid}/accounts/{userId:guid}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessionsAsync(Guid organizationId, Guid userId, PlatformReasonRequest request, CancellationToken ct) =>
        await platform.RevokeSessionsAsync(organizationId, userId, request.Reason,
            audit.Create("platform.account.sessions_revoked", "platform.accounts", userId, request.Reason), ct) ? NoContent() : NotFound();
}
