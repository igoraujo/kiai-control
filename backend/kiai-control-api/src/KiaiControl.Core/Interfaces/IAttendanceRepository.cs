using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
