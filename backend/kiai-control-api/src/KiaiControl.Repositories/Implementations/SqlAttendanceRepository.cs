using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Attendance;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlAttendanceRepository(IDbConnectionFactory dbConnectionFactory) : IAttendanceRepository
{
    public async Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        attendance.Id = Guid.NewGuid();
        attendance.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<AttendanceRecord>(AttendanceQueries.Insert, attendance);
        return created;
    }

    public async Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var parameters = new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        };

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<AttendanceRecord>(AttendanceQueries.ListByOrganization, parameters);
        return items.ToList();
    }

    public async Task<AttendanceRecord?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AttendanceRecord>(AttendanceQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<AttendanceRecord?> UpdateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        attendance.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AttendanceRecord>(AttendanceQueries.Update, attendance);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(AttendanceQueries.Delete, new { OrganizationId = organizationId, Id = id });
        return affected > 0;
    }
}
