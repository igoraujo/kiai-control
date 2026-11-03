using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Graduation;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Graduation;
using KiaiControl.UseCases.Modalities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "owner,admin,teacher")]
[Authorize(Policy = PermissionCodes.GraduationManage)]
[Route("api/v1")]
public sealed class GraduationController(
    OrganizationContext organizationContext,
    GraduationUseCase graduationUseCase,
    ModalityUseCase modalityUseCase,
    ClientUseCase clientUseCase) : ControllerBase
{
    [HttpGet("graduation/options")]
    public async Task<ActionResult<GraduationOptionsResponse>> OptionsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var modalitiesTask = modalityUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        var clientsTask = clientUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        await Task.WhenAll(modalitiesTask, clientsTask);
        return Ok(new GraduationOptionsResponse
        {
            Modalities = (await modalitiesTask)
                .Where(modality => modality.IsActive && modality.Type == "martial_art")
                .ToList(),
            Clients = (await clientsTask).Where(client => client.Status == "active").ToList()
        });
    }

    [HttpPost("graduation-systems")]
    public async Task<ActionResult<GraduationSystemResponse>> CreateSystemAsync(
        [FromBody] CreateGraduationSystemRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var system = await graduationUseCase.CreateSystemAsync(organizationId, request, cancellationToken);
            return system is null ? NotFound(SystemMessages.AModalidadeNaoFoiEncontradaEstaInativaOuNaoEUmaArteMarcialNestaOrganizacao)
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var level = await graduationUseCase.CreateLevelAsync(organizationId, systemId, request, cancellationToken);
            return level is null ? NotFound(SystemMessages.TheActiveGraduationSystemWasNotFoundInThisOrganization)
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await graduationUseCase.ListLevelsAsync(organizationId, systemId, cancellationToken));
    }

    [HttpGet("graduation-systems/{systemId:guid}/belts")]
    public async Task<ActionResult<IReadOnlyList<GraduationBeltResponse>>> ListBeltsAsync(
        Guid systemId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            return Ok(await graduationUseCase.ListBeltsAsync(organizationId, systemId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("graduation-systems/{systemId:guid}/belts")]
    public async Task<ActionResult<GraduationBeltResponse>> CreateBeltAsync(
        Guid systemId,
        [FromBody] CreateGraduationBeltRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var belt = await graduationUseCase.CreateBeltAsync(organizationId, systemId, request, cancellationToken);
            return belt is null
                ? Conflict(SystemMessages.AFaixaJaExisteOuOSistemaDeGraduacaoNaoEstaDisponivelNestaOrganizacao)
                : StatusCode(StatusCodes.Status201Created, belt);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("graduation-systems/{systemId:guid}/belts/{beltId:guid}")]
    public async Task<IActionResult> UpdateBeltAsync(
        Guid systemId,
        Guid beltId,
        [FromBody] CreateGraduationBeltRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var updated = await graduationUseCase.UpdateBeltAsync(
                organizationId, systemId, beltId, request, cancellationToken);
            return updated ? NoContent() : Conflict(SystemMessages.AFaixaNaoExisteTemNomeDuplicadoOuAQuantidadeDeGrausEMenorQueOsGrausJaCadastrados);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("graduation-systems/{systemId:guid}/belts/{beltId:guid}")]
    public async Task<IActionResult> DeleteBeltAsync(
        Guid systemId,
        Guid beltId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var deleted = await graduationUseCase.DeleteBeltAsync(organizationId, systemId, beltId, cancellationToken);
            return deleted ? NoContent() : Conflict(SystemMessages.AFaixaNaoExisteOuJaPossuiGraduacoesRegistradasCorrijaOuRemovaPrimeiroOsGrausNaoUtilizados);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("graduation-systems/{systemId:guid}/belts/{beltId:guid}/degrees")]
    public async Task<ActionResult<GraduationLevelResponse>> CreateDegreeAsync(
        Guid systemId,
        Guid beltId,
        [FromBody] CreateGraduationDegreeRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var degree = await graduationUseCase.CreateDegreeAsync(
                organizationId, systemId, beltId, request, cancellationToken);
            return degree is null
                ? Conflict(SystemMessages.AFaixaNaoFoiEncontradaOuTodosOsGrausConfiguradosJaForamCadastrados)
                : StatusCode(StatusCodes.Status201Created, degree);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("graduation-systems/{systemId:guid}/belts/{beltId:guid}/degrees/{degreeId:guid}")]
    public async Task<IActionResult> UpdateDegreeAsync(
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        [FromBody] CreateGraduationDegreeRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var updated = await graduationUseCase.UpdateDegreeAsync(
                organizationId, systemId, beltId, degreeId, request, cancellationToken);
            return updated ? NoContent() : NotFound(SystemMessages.OGrauNaoFoiEncontradoNestaFaixaEOrganizacao);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("graduation-systems/{systemId:guid}/belts/{beltId:guid}/degrees/{degreeId:guid}")]
    public async Task<IActionResult> DeleteDegreeAsync(
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var deleted = await graduationUseCase.DeleteDegreeAsync(
                organizationId, systemId, beltId, degreeId, cancellationToken);
            return deleted ? NoContent() : Conflict(SystemMessages.SoEPossivelExcluirOUltimoGrauDaSequenciaEEleNaoPodeTerSidoUsadoEmGraduacoes);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("clients/{clientId:guid}/graduations")]
    public async Task<ActionResult<ClientGraduationResponse>> SetInitialAsync(
        Guid clientId,
        [FromBody] SetClientGraduationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var graduation = await graduationUseCase.SetInitialAsync(organizationId, clientId, request, cancellationToken);
            return graduation is null ? Conflict(SystemMessages.OAlunoNaoEstaVinculadoAModalidadeONivelNaoEstaDisponivelOuJaExisteUmaGraduacao)
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var graduationEvent = await graduationUseCase.PromoteAsync(organizationId, clientId, request, cancellationToken);
            return graduationEvent is null
                ? Conflict(SystemMessages.PromotionIsInvalidLevelMustAdvanceTheCurrentRankAndAnyManualEvaluationMustPass)
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
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