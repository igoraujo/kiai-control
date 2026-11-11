using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/v1/workspace")]
public sealed class WorkspaceController(OrganizationContext organizationContext, ModalityUseCase modalityUseCase, KiaiControl.UseCases.Organizations.OrganizationUseCase? organizations = null) : ControllerBase
{
    [HttpGet("capabilities")]
    public async Task<ActionResult<WorkspaceCapabilitiesResponse>> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var activeTypes = await modalityUseCase.ListActiveTypesAsync(organizationId, cancellationToken);
        var hasMartialArts = activeTypes.Contains("martial_art", StringComparer.OrdinalIgnoreCase);
        var hasFitness = activeTypes.Contains("fitness", StringComparer.OrdinalIgnoreCase);
        var hasHybrid = activeTypes.Contains("hybrid", StringComparer.OrdinalIgnoreCase);
        return Ok(new WorkspaceCapabilitiesResponse { OrganizationName = (organizations is null ? null : await organizations.GetByIdAsync(organizationId, cancellationToken))?.Name ?? "Academia", HasMartialArts = hasMartialArts || hasHybrid, HasFitnessActivities = hasFitness || hasHybrid, HasGraduation = hasMartialArts });
    }
}
