using KiaiControl.Contracts.Organizations;
using KiaiControl.UseCases.Organizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "platform_owner")]
[Route("api/v1/platform/organizations")]
public sealed class PlatformOrganizationsController(OrganizationUseCase organizationUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await organizationUseCase.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationRegistrationResponse>> RegisterAsync(
        [FromBody] RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var registration = await organizationUseCase.RegisterAsync(request, cancellationToken);
            return registration is null
                ? Conflict(SystemMessages.TheOrganizationDocumentOrOwnerEMailIsAlreadyRegistered)
                : StatusCode(StatusCodes.Status201Created, registration);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
