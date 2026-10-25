using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IClassGroupRepository
{
    Task<ClassGroup?> CreateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassGroup>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ClassGroup?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<ClassGroup?> UpdateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<ClassSchedule?> CreateScheduleAsync(
        Guid organizationId,
        ClassSchedule schedule,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteScheduleAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<Enrollment?> CreateEnrollmentAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken = default);

    Task<bool> CancelEnrollmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<bool> AssignTeacherAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        bool isPrimary,
        DateOnly startsAt,
        DateOnly? endsAt,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveTeacherAssignmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        DateOnly startsAt,
        CancellationToken cancellationToken = default);
}