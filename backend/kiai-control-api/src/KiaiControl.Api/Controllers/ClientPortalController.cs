using System.Security.Claims;
using KiaiControl.Contracts.Clients;
using KiaiControl.Contracts.Fitness;
using KiaiControl.Contracts.Lessons;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Fitness;
using KiaiControl.UseCases.Lessons;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "client")]
[Route("api/v1/mobile")]
public sealed class ClientPortalController(
    OrganizationContext organizationContext,
    ClientUseCase clientUseCase,
    ModalityUseCase modalityUseCase,
    LessonUseCase lessonUseCase,
    FitnessUseCase fitnessUseCase) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<ClientPortalProfileResponse>> GetProfileAsync(CancellationToken cancellationToken)
    {
        if (!TryGetClientContext(out var organizationId, out var personId))
        {
            return Unauthorized();
        }

        var client = await clientUseCase.GetByPersonIdAsync(organizationId, personId, cancellationToken);
        if (client is null || client.Status != "active")
        {
            return NotFound();
        }

        return Ok(new ClientPortalProfileResponse
        {
            ClientId = client.Id,
            FullName = client.FullName,
            PreferredName = client.PreferredName,
            Email = client.Email,
            Phone = client.Phone,
            JoinedAt = client.JoinedAt,
            Status = client.Status
        });
    }

    [HttpGet("modalities")]
    public async Task<ActionResult<IReadOnlyList<ModalityResponse>>> GetModalitiesAsync(CancellationToken cancellationToken)
    {
        var (organizationId, client) = await GetActiveClientAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (client is null)
        {
            return NotFound();
        }

        return Ok(await modalityUseCase.ListClientModalitiesAsync(organizationId, client.Id, cancellationToken));
    }

    [HttpGet("lessons")]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> GetLessonsAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var (organizationId, client) = await GetActiveClientAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (client is null)
        {
            return NotFound();
        }

        var rangeStart = from?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var rangeEnd = to?.ToUniversalTime() ?? rangeStart.AddDays(30);
        if (rangeEnd <= rangeStart || rangeEnd - rangeStart > TimeSpan.FromDays(31))
        {
            return BadRequest("O período deve ser válido e não pode exceder 31 dias.");
        }

        var modalities = await modalityUseCase.ListClientModalitiesAsync(organizationId, client.Id, cancellationToken);
        if (modalities.Count == 0)
        {
            return Ok(Array.Empty<LessonResponse>());
        }

        var modalityIds = modalities.Select(modality => modality.Id).ToHashSet();
        var lessons = new List<LessonResponse>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var result = await lessonUseCase.ListAsync(
                organizationId, page, pageSize, cancellationToken, from: rangeStart, to: rangeEnd);
            lessons.AddRange(result.Where(lesson =>
                modalityIds.Contains(lesson.ModalityId)
                && lesson.Status is "scheduled" or "rescheduled"));
            if (result.Count < pageSize)
            {
                break;
            }
        }

        return Ok(lessons.OrderBy(lesson => lesson.StartsAt).ToArray());
    }

    [HttpGet("fitness")]
    public async Task<ActionResult<FitnessSummaryResponse>> GetFitnessAsync(CancellationToken cancellationToken)
    {
        var (organizationId, client) = await GetActiveClientAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (client is null)
        {
            return NotFound();
        }

        return Ok(await fitnessUseCase.SummaryAsync(organizationId, client.Id, cancellationToken));
    }

    private async Task<(Guid OrganizationId, ClientResponse? Client)> GetActiveClientAsync(CancellationToken cancellationToken)
    {
        if (!TryGetClientContext(out var organizationId, out var personId))
        {
            return (Guid.Empty, null);
        }

        var found = await clientUseCase.GetByPersonIdAsync(organizationId, personId, cancellationToken);
        if (found is null || found.Status != "active")
        {
            return (organizationId, null);
        }

        return (organizationId, found);
    }

    private bool TryGetClientContext(out Guid organizationId, out Guid personId)
    {
        organizationId = organizationContext.OrganizationId ?? Guid.Empty;
        personId = Guid.Empty;
        return organizationId != Guid.Empty
            && Guid.TryParse(User.FindFirstValue("person_id"), out personId)
            && personId != Guid.Empty;
    }
}
