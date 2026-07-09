using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> UpdateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
}
