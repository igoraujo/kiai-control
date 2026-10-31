using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IModalityScheduleRepository
{
    Task<ModalitySchedule?> CreateAsync(ModalitySchedule schedule, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModalitySchedule>> ListAsync(
        Guid organizationId,
        Guid modalityId,
        CancellationToken cancellationToken = default);

    Task<bool> ModalityExistsAsync(
        Guid organizationId,
        Guid modalityId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid organizationId,
        Guid modalityId,
        Guid scheduleId,
        CancellationToken cancellationToken = default);
}
