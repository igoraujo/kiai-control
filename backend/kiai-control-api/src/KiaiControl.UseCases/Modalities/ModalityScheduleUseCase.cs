using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Modalities;

public sealed class ModalityScheduleUseCase(IModalityScheduleRepository scheduleRepository)
{
    public async Task<ModalityScheduleResponse?> CreateAsync(
        Guid organizationId,
        Guid modalityId,
        CreateModalityScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || modalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.AOrganizacaoEAModalidadeSaoObrigatorias);
        }

        if (request.DayOfWeek is < 0 or > 6)
        {
            throw new ArgumentException(SystemMessages.SelecioneUmDiaDaSemanaValido);
        }

        if (request.EndsAt <= request.StartsAt)
        {
            throw new ArgumentException(SystemMessages.OHorarioDeTerminoDeveSerPosteriorAoHorarioDeInicio);
        }

        var created = await scheduleRepository.CreateAsync(new ModalitySchedule
        {
            OrganizationId = organizationId,
            ModalityId = modalityId,
            BranchId = request.BranchId,
            TeacherId = request.TeacherId,
            DayOfWeek = request.DayOfWeek,
            StartsAt = request.StartsAt.ToTimeSpan(),
            EndsAt = request.EndsAt.ToTimeSpan()
        }, cancellationToken);

        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<ModalityScheduleResponse>?> ListAsync(
        Guid organizationId,
        Guid modalityId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || modalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.AOrganizacaoEAModalidadeSaoObrigatorias);
        }

        var schedules = await scheduleRepository.ListAsync(organizationId, modalityId, cancellationToken);
        return schedules.Count == 0
            && !await scheduleRepository.ModalityExistsAsync(organizationId, modalityId, cancellationToken)
                ? null
                : schedules.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ModalityScheduleResponse>> ListByTeacherAsync(
        Guid organizationId,
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || teacherId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.AOrganizacaoEOProfessorSaoObrigatorios);
        }

        var schedules = await scheduleRepository.ListByTeacherAsync(organizationId, teacherId, cancellationToken);
        return schedules.Select(ToResponse).ToList();
    }

    public Task<bool> DeactivateAsync(
        Guid organizationId,
        Guid modalityId,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || modalityId == Guid.Empty || scheduleId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.AOrganizacaoAModalidadeEOHorarioSaoObrigatorios);
        }

        return scheduleRepository.DeactivateAsync(organizationId, modalityId, scheduleId, cancellationToken);
    }

    private static ModalityScheduleResponse ToResponse(ModalitySchedule schedule) => new()
    {
        Id = schedule.Id,
        OrganizationId = schedule.OrganizationId,
        ModalityId = schedule.ModalityId,
        ModalityName = schedule.ModalityName,
        BranchId = schedule.BranchId,
        TeacherId = schedule.TeacherId,
        BranchName = schedule.BranchName,
        TeacherName = schedule.TeacherName,
        DayOfWeek = schedule.DayOfWeek,
        StartsAt = TimeOnly.FromTimeSpan(schedule.StartsAt),
        EndsAt = TimeOnly.FromTimeSpan(schedule.EndsAt),
        IsActive = schedule.IsActive
    };
}
