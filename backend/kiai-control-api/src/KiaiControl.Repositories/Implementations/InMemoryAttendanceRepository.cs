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

    public Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _attendance
            .Where(record => record.OrganizationId == organizationId)
            .OrderByDescending(record => record.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(items);
    }

    public Task<AttendanceRecord?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var attendance = _attendance.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(attendance);
    }

    public Task<AttendanceRecord?> UpdateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        var index = _attendance.FindIndex(item => item.OrganizationId == attendance.OrganizationId && item.Id == attendance.Id);
        if (index < 0)
        {
            return Task.FromResult<AttendanceRecord?>(null);
        }

        var current = _attendance[index];
        current.LessonId = attendance.LessonId;
        current.ClientId = attendance.ClientId;
        current.AttendanceDate = attendance.AttendanceDate;
        current.Status = attendance.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<AttendanceRecord?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _attendance.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}
