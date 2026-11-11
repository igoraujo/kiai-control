using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Ranking;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Modalities;
using KiaiControl.UseCases.Ranking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController]
[Authorize(Roles = "owner,admin,teacher")]
[Authorize(Policy = PermissionCodes.RankingManage)]
[Route("api/v1")]
public sealed class RankingController(OrganizationContext organizationContext, RankingUseCase rankingUseCase, ClientUseCase clientUseCase, ModalityUseCase modalityUseCase) : ControllerBase
{
    [HttpGet("rankings/options")]
    public async Task<ActionResult<RankingOptionsResponse>> OptionsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var clientsTask = clientUseCase.ListAllOptionsAsync(organizationId, cancellationToken);
        var modalitiesTask = modalityUseCase.ListAllOptionsAsync(organizationId, cancellationToken);
        await Task.WhenAll(clientsTask, modalitiesTask);
        return Ok(new RankingOptionsResponse { Clients = (await clientsTask).Where(client => client.Status == "active").ToList(), Modalities = (await modalitiesTask).Where(modality => modality.IsActive).ToList() });
    }

    [HttpPost("ranking-cycles")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<RankingCycleResponse>> CreateCycleAsync([FromBody] CreateRankingCycleRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var cycle = await rankingUseCase.CreateCycleAsync(organizationId, request, cancellationToken);
            return cycle is null ? Conflict(SystemMessages.TheModalityOrClassGroupIsUnavailableInThisOrganization) : StatusCode(StatusCodes.Status201Created, cycle);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("rankings/history")]
    public async Task<ActionResult<IReadOnlyList<RankingCycleResponse>>> ListCyclesAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await rankingUseCase.ListCyclesAsync(organizationId, page, pageSize, cancellationToken));
    }

    [HttpPost("ranking-cycles/{cycleId:guid}/recalculate")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> RecalculateAsync(Guid cycleId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var entries = await rankingUseCase.RecalculateAsync(organizationId, cycleId, cancellationToken);
        return entries is null ? NotFound(SystemMessages.AnOpenRankingCycleWasNotFound) : Ok(entries);
    }

    [HttpGet("rankings/current")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> GetCurrentAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await rankingUseCase.GetCurrentAsync(organizationId, DateOnly.FromDateTime(DateTime.UtcNow), page, pageSize, cancellationToken));
    }

    [HttpGet("ranking-cycles/{cycleId:guid}/entries")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> GetByCycleAsync(Guid cycleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await rankingUseCase.GetByCycleAsync(organizationId, cycleId, page, pageSize, cancellationToken));
    }

    [HttpPost("medals")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<MedalResponse>> CreateMedalAsync([FromBody] CreateMedalRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var medal = await rankingUseCase.CreateMedalAsync(organizationId, request, cancellationToken);
            return medal is null ? Conflict(SystemMessages.AMedalWithThisCodeAlreadyExists) : StatusCode(StatusCodes.Status201Created, medal);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("medals")]
    public async Task<ActionResult<IReadOnlyList<MedalResponse>>> ListMedalsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await rankingUseCase.ListMedalsAsync(organizationId, cancellationToken));
    }

    [HttpPost("medals/{medalId:guid}/awards")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<PersonMedalResponse>> AwardMedalAsync(Guid medalId, [FromBody] AwardMedalRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var award = await rankingUseCase.AwardMedalAsync(organizationId, medalId, request, cancellationToken);
            return award is null ? NotFound(SystemMessages.TheMedalOrRecipientIsUnavailableInThisOrganization) : StatusCode(StatusCodes.Status201Created, award);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("clients/{clientId:guid}/medals")]
    public async Task<ActionResult<IReadOnlyList<PersonMedalResponse>>> ListClientMedalsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await rankingUseCase.ListClientMedalsAsync(organizationId, clientId, cancellationToken));
    }
}
