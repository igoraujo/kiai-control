using KiaiControl.Contracts.Ranking;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Ranking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,teacher")]
[Route("api/v1")]
public sealed class RankingController(OrganizationContext organizationContext, RankingUseCase rankingUseCase) : ControllerBase
{
    [HttpPost("ranking-cycles")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<RankingCycleResponse>> CreateCycleAsync(
        [FromBody] CreateRankingCycleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var cycle = await rankingUseCase.CreateCycleAsync(organizationId, request, cancellationToken);
            return cycle is null ? Conflict("The modality or class group is unavailable in this organization.")
                : StatusCode(StatusCodes.Status201Created, cycle);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("rankings/history")]
    public async Task<ActionResult<IReadOnlyList<RankingCycleResponse>>> ListCyclesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await rankingUseCase.ListCyclesAsync(organizationId, page, pageSize, cancellationToken));
    }

    [HttpPost("ranking-cycles/{cycleId:guid}/recalculate")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> RecalculateAsync(Guid cycleId, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        var entries = await rankingUseCase.RecalculateAsync(organizationId, cycleId, cancellationToken);
        return entries is null ? NotFound("An open ranking cycle was not found.") : Ok(entries);
    }

    [HttpGet("rankings/current")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> GetCurrentAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await rankingUseCase.GetCurrentAsync(organizationId, DateOnly.FromDateTime(DateTime.UtcNow), page, pageSize, cancellationToken));
    }

    [HttpGet("ranking-cycles/{cycleId:guid}/entries")]
    public async Task<ActionResult<IReadOnlyList<RankingEntryResponse>>> GetByCycleAsync(
        Guid cycleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        return Ok(await rankingUseCase.GetByCycleAsync(organizationId, cycleId, page, pageSize, cancellationToken));
    }

    [HttpPost("medals")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<MedalResponse>> CreateMedalAsync([FromBody] CreateMedalRequest request, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var medal = await rankingUseCase.CreateMedalAsync(organizationId, request, cancellationToken);
            return medal is null ? Conflict("A medal with this code already exists.") : StatusCode(StatusCodes.Status201Created, medal);
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
            return BadRequest("Organization context is required.");
        }

        return Ok(await rankingUseCase.ListMedalsAsync(organizationId, cancellationToken));
    }

    [HttpPost("medals/{medalId:guid}/awards")]
    [Authorize(Roles = "owner,admin,teacher")]
    public async Task<ActionResult<PersonMedalResponse>> AwardMedalAsync(
        Guid medalId,
        [FromBody] AwardMedalRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var award = await rankingUseCase.AwardMedalAsync(organizationId, medalId, request, cancellationToken);
            return award is null ? NotFound("The medal or recipient is unavailable in this organization.")
                : StatusCode(StatusCodes.Status201Created, award);
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
            return BadRequest("Organization context is required.");
        }

        return Ok(await rankingUseCase.ListClientMedalsAsync(organizationId, clientId, cancellationToken));
    }
}