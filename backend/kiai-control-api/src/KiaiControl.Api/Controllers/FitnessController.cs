using System.Security.Claims;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Fitness;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize(Roles = "owner,admin,teacher")]
[Authorize(Policy = PermissionCodes.FitnessManage)]
[Route("api/v1/fitness-goals")]
public sealed class FitnessController(OrganizationContext organizationContext, FitnessUseCase fitnessUseCase, ClientUseCase clientUseCase, ModalityUseCase modalityUseCase) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<FitnessOptionsResponse>> OptionsAsync(CancellationToken cancellationToken, string? area = null)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        string selectedArea;
        try { selectedArea = AcademyArea.Normalize(area); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
        var clientsTask = clientUseCase.ListAllOptionsAsync(organizationId, cancellationToken);
        var modalitiesTask = modalityUseCase.ListAllOptionsAsync(organizationId, cancellationToken);
        await Task.WhenAll(clientsTask, modalitiesTask);
        return Ok(new FitnessOptionsResponse { Clients = (await clientsTask).Where(client => client.Status == "active" && (selectedArea == AcademyArea.General || client.Areas.Contains(selectedArea))).ToList(), Modalities = (await modalitiesTask).Where(modality => modality.IsActive && AcademyArea.IncludesModality(selectedArea, modality.Type)).ToList() });
    }

    [HttpPost]
    public async Task<ActionResult<FitnessGoalResponse>> CreateAsync([FromBody] CreateFitnessGoalRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var goal = await fitnessUseCase.CreateAsync(organizationId, request, cancellationToken);
            return goal is null ? NotFound(SystemMessages.TheClientOrModalityIsUnavailableInThisOrganization) : StatusCode(StatusCodes.Status201Created, goal);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FitnessGoalResponse>>> ListAsync([FromQuery] Guid? clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default, string? area = null)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try { return Ok(await fitnessUseCase.ListAsync(organizationId, clientId, page, pageSize, cancellationToken, area)); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }

    [HttpPost("{goalId:guid}/progress")]
    public async Task<ActionResult<FitnessProgressResponse>> RecordProgressAsync(Guid goalId, [FromBody] RecordFitnessProgressRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var actorPersonId = Guid.TryParse(User.FindFirstValue("person_id"), out var personId) ? personId : (Guid? )null;
        try
        {
            var progress = await fitnessUseCase.RecordProgressAsync(organizationId, goalId, actorPersonId, request, cancellationToken);
            return progress is null ? NotFound(SystemMessages.TheActiveFitnessGoalWasNotFoundInThisOrganization) : StatusCode(StatusCodes.Status201Created, progress);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("{goalId:guid}/progress")]
    public async Task<ActionResult<IReadOnlyList<FitnessProgressResponse>>> ListProgressAsync(Guid goalId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            return Ok(await fitnessUseCase.ListProgressAsync(organizationId, goalId, page, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPatch("{goalId:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid goalId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var canceled = await fitnessUseCase.CancelAsync(organizationId, goalId, cancellationToken);
        return canceled ? NoContent() : NotFound();
    }

    [HttpGet("clients/{clientId:guid}/summary")]
    public async Task<ActionResult<FitnessSummaryResponse>> SummaryAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            return Ok(await fitnessUseCase.SummaryAsync(organizationId, clientId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
