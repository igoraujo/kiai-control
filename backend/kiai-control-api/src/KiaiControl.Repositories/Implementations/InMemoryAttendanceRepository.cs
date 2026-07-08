using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryAttendanceRepository : IAttendanceRepository
{
    private readonly List<AttendanceRecord> _attendance = [];

    public Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        attendance.Id = Guid.NewGuid();
        attendance.CreatedAt = DateTimeOffset.UtcNow;
        _attendance.Add(attendance);
        return Task.FromResult(attendance);
    }

    public Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = _attendance.Where(record => record.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(items);
    }
}
