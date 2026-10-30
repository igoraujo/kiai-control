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

    public async Task<AttendanceRecord?> CreateCheckInAsync(
        Guid organizationId,
        Guid personId,
        Guid lessonId,
        DateTimeOffset checkedInAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AttendanceRecord>(AttendanceQueries.CreateCheckIn, new
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PersonId = personId,
            LessonId = lessonId,
            CheckedInAt = checkedInAt,
            CreatedAt = checkedInAt
        });
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

    public async Task<IReadOnlyList<AttendanceRecord>> ListByLessonAsync(
        Guid organizationId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var entries = await connection.QueryAsync<AttendanceRecord>(
            AttendanceQueries.ListByLesson,
            new { OrganizationId = organizationId, LessonId = lessonId });
        return entries.ToList();
    }

    public async Task<IReadOnlyList<AttendanceRecord>> ListPendingByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var entries = await connection.QueryAsync<AttendanceRecord>(AttendanceQueries.ListPendingByOrganization, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return entries.ToList();
    }

    public async Task<AttendanceRecord?> ValidateCheckInAsync(
        Guid organizationId,
        Guid attendanceId,
        Guid validatorPersonId,
        string status,
        DateTimeOffset validatedAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AttendanceRecord>(AttendanceQueries.ValidateCheckIn, new
        {
            OrganizationId = organizationId,
            AttendanceId = attendanceId,
            ValidatedByPersonId = validatorPersonId,
            Status = status,
            ValidatedAt = validatedAt
        });
    }

    public async Task<IReadOnlyList<AttendanceRecord>?> SaveLessonCallAsync(
        Guid organizationId,
        Guid lessonId,
        Guid validatorPersonId,
        IReadOnlyList<AttendanceRecord> entries,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var classGroupId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            AttendanceQueries.LockLessonForCall,
            new { OrganizationId = organizationId, LessonId = lessonId },
            transaction);
        if (classGroupId is null)
        {
            return null;
        }

        var savedEntries = new List<AttendanceRecord>(entries.Count);
        foreach (var entry in entries)
        {
            entry.Id = Guid.NewGuid();
            entry.OrganizationId = organizationId;
            entry.LessonId = lessonId;
            entry.CreatedAt = DateTimeOffset.UtcNow;
            entry.ValidatedAt = entry.CreatedAt;
            entry.ValidatedByPersonId = validatorPersonId;

            var saved = await connection.QuerySingleOrDefaultAsync<AttendanceRecord>(
                AttendanceQueries.UpsertCallEntry,
                entry,
                transaction);
            if (saved is null)
            {
                return null;
            }

            savedEntries.Add(saved);
        }

        transaction.Commit();
        return savedEntries;
    }
}
