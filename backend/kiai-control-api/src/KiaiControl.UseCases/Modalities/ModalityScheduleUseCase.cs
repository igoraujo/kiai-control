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
            throw new ArgumentException("A organização e a modalidade são obrigatórias.");
        }

        if (request.DayOfWeek is < 0 or > 6)
        {
            throw new ArgumentException("Selecione um dia da semana válido.");
        }

        if (request.EndsAt <= request.StartsAt)
        {
            throw new ArgumentException("O horário de término deve ser posterior ao horário de início.");
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
            throw new ArgumentException("A organização e a modalidade são obrigatórias.");
        }

        var schedules = await scheduleRepository.ListAsync(organizationId, modalityId, cancellationToken);
        return schedules.Count == 0
            && !await scheduleRepository.ModalityExistsAsync(organizationId, modalityId, cancellationToken)
                ? null
                : schedules.Select(ToResponse).ToList();
    }

    public Task<bool> DeactivateAsync(
        Guid organizationId,
        Guid modalityId,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || modalityId == Guid.Empty || scheduleId == Guid.Empty)
        {
            throw new ArgumentException("A organização, a modalidade e o horário são obrigatórios.");
        }

        return scheduleRepository.DeactivateAsync(organizationId, modalityId, scheduleId, cancellationToken);
    }

    private static ModalityScheduleResponse ToResponse(ModalitySchedule schedule) => new()
    {
        Id = schedule.Id,
        OrganizationId = schedule.OrganizationId,
        ModalityId = schedule.ModalityId,
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
