using KiaiControl.Contracts.Graduation;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Graduation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,teacher")]
[Route("api/v1")]
public sealed class GraduationController(OrganizationContext organizationContext, GraduationUseCase graduationUseCase) : ControllerBase
{
    [HttpPost("graduation-systems")]
    public async Task<ActionResult<GraduationSystemResponse>> CreateSystemAsync(
        [FromBody] CreateGraduationSystemRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var system = await graduationUseCase.CreateSystemAsync(organizationId, request, cancellationToken);
            return system is null ? NotFound("The active modality was not found in this organization.")
                : StatusCode(StatusCodes.Status201Created, system);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("graduation-systems")]
    public async Task<ActionResult<IReadOnlyList<GraduationSystemResponse>>> ListSystemsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await graduationUseCase.ListSystemsAsync(organizationId, cancellationToken));
    }

    [HttpPost("graduation-systems/{systemId:guid}/levels")]
    public async Task<ActionResult<GraduationLevelResponse>> CreateLevelAsync(
        Guid systemId,
        [FromBody] CreateGraduationLevelRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var level = await graduationUseCase.CreateLevelAsync(organizationId, systemId, request, cancellationToken);
            return level is null ? NotFound("The active graduation system was not found in this organization.")
                : StatusCode(StatusCodes.Status201Created, level);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("graduation-systems/{systemId:guid}/levels")]
    public async Task<ActionResult<IReadOnlyList<GraduationLevelResponse>>> ListLevelsAsync(Guid systemId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await graduationUseCase.ListLevelsAsync(organizationId, systemId, cancellationToken));
    }

    [HttpPost("clients/{clientId:guid}/graduations")]
    public async Task<ActionResult<ClientGraduationResponse>> SetInitialAsync(
        Guid clientId,
        [FromBody] SetClientGraduationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var graduation = await graduationUseCase.SetInitialAsync(organizationId, clientId, request, cancellationToken);
            return graduation is null ? Conflict("Client, modality or level is unavailable, or a graduation already exists.")
                : StatusCode(StatusCodes.Status201Created, graduation);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("clients/{clientId:guid}/graduation-events")]
    public async Task<ActionResult<GraduationEventResponse>> PromoteAsync(
        Guid clientId,
        [FromBody] PromoteClientRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var graduationEvent = await graduationUseCase.PromoteAsync(organizationId, clientId, request, cancellationToken);
            return graduationEvent is null
                ? Conflict("Promotion is invalid: level must advance the current rank and any manual evaluation must pass.")
                : StatusCode(StatusCodes.Status201Created, graduationEvent);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("clients/{clientId:guid}/graduation-events")]
    public async Task<ActionResult<IReadOnlyList<GraduationEventResponse>>> ListEventsAsync(
        Guid clientId,
        [FromQuery] Guid modalityId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await graduationUseCase.ListEventsAsync(organizationId, clientId, modalityId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("graduation/eligible-clients")]
    public async Task<ActionResult<IReadOnlyList<EligibleClientResponse>>> ListEligibleAsync(
        [FromQuery] Guid systemId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            return Ok(await graduationUseCase.ListEligibleAsync(organizationId, systemId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}