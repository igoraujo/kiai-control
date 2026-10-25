using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> CreateAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Lesson?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Lesson?> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task<Lesson?> CancelAsync(
        Guid organizationId,
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<Lesson?> RescheduleAsync(
        Guid organizationId,
        Guid id,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lesson>?> GenerateFromSchedulesAsync(
        Guid organizationId,
        Guid classGroupId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}