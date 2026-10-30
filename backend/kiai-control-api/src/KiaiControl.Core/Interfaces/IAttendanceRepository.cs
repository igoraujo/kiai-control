using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> CreateCheckInAsync(
        Guid organizationId,
        Guid personId,
        Guid lessonId,
        DateTimeOffset checkedInAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> UpdateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListByLessonAsync(
        Guid organizationId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListPendingByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> ValidateCheckInAsync(
        Guid organizationId,
        Guid attendanceId,
        Guid validatorPersonId,
        string status,
        DateTimeOffset validatedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>?> SaveLessonCallAsync(
        Guid organizationId,
        Guid lessonId,
        Guid validatorPersonId,
        IReadOnlyList<AttendanceRecord> entries,
        CancellationToken cancellationToken = default);
}
