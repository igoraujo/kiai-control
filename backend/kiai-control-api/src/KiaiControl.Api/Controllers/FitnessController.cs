using System.Security.Claims;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Fitness;
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
public sealed class FitnessController(
    OrganizationContext organizationContext,
    FitnessUseCase fitnessUseCase,
    ClientUseCase clientUseCase,
    ModalityUseCase modalityUseCase) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<FitnessOptionsResponse>> OptionsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var clientsTask = clientUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        var modalitiesTask = modalityUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        await Task.WhenAll(clientsTask, modalitiesTask);
        return Ok(new FitnessOptionsResponse
        {
            Clients = (await clientsTask).Where(client => client.Status == "active").ToList(),
            Modalities = (await modalitiesTask).Where(modality => modality.IsActive).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<FitnessGoalResponse>> CreateAsync(
        [FromBody] CreateFitnessGoalRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var goal = await fitnessUseCase.CreateAsync(organizationId, request, cancellationToken);
            return goal is null ? NotFound("The client or modality is unavailable in this organization.")
                : StatusCode(StatusCodes.Status201Created, goal);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FitnessGoalResponse>>> ListAsync(
        [FromQuery] Guid? clientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await fitnessUseCase.ListAsync(organizationId, clientId, page, pageSize, cancellationToken));
    }

    [HttpPost("{goalId:guid}/progress")]
    public async Task<ActionResult<FitnessProgressResponse>> RecordProgressAsync(
        Guid goalId,
        [FromBody] RecordFitnessProgressRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var teacherId = Guid.TryParse(User.FindFirstValue("person_id"), out var personId) ? personId : (Guid?)null;
        try
        {
            var progress = await fitnessUseCase.RecordProgressAsync(organizationId, goalId, teacherId, request, cancellationToken);
            return progress is null ? NotFound("The active fitness goal was not found in this organization.")
                : StatusCode(StatusCodes.Status201Created, progress);
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
            return BadRequest("Organization context is required.");
        }

        var canceled = await fitnessUseCase.CancelAsync(organizationId, goalId, cancellationToken);
        return canceled ? NoContent() : NotFound();
    }

    [HttpGet("clients/{clientId:guid}/summary")]
    public async Task<ActionResult<FitnessSummaryResponse>> SummaryAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
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