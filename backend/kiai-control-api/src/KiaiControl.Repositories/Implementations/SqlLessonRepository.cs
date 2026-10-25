using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Lessons;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlLessonRepository(IDbConnectionFactory dbConnectionFactory) : ILessonRepository
{
    public async Task<Lesson?> CreateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        lesson.Id = Guid.NewGuid();
        lesson.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Lesson>(LessonQueries.Insert, lesson);
    }

    public async Task<IReadOnlyList<Lesson>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
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
        var lessons = await connection.QueryAsync<Lesson>(LessonQueries.ListByOrganization, parameters);
        return lessons.ToList();
    }

    public async Task<Lesson?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Lesson>(LessonQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Lesson?> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Lesson>(LessonQueries.Update, lesson);
    }

    public async Task<Lesson?> CancelAsync(
        Guid organizationId,
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Lesson>(
            LessonQueries.Cancel,
            new { OrganizationId = organizationId, Id = id, Reason = reason, UpdatedAt = DateTimeOffset.UtcNow });
    }

    public async Task<Lesson?> RescheduleAsync(
        Guid organizationId,
        Guid id,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Lesson>(
            LessonQueries.Reschedule,
            new { OrganizationId = organizationId, Id = id, StartsAt = startsAt, EndsAt = endsAt, UpdatedAt = DateTimeOffset.UtcNow });
    }

    public async Task<IReadOnlyList<Lesson>?> GenerateFromSchedulesAsync(
        Guid organizationId,
        Guid classGroupId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QueryAsync<Lesson>(
            LessonQueries.GenerateFromSchedules,
            new { OrganizationId = organizationId, ClassGroupId = classGroupId, StartDate = startDate, EndDate = endDate });

        if (created.Any())
        {
            return created.ToList();
        }

        var classGroupExists = await connection.QuerySingleOrDefaultAsync<bool?>(
            "select true from class_groups where id = @ClassGroupId and organization_id = @OrganizationId and status = 'active';",
            new { OrganizationId = organizationId, ClassGroupId = classGroupId });
        return classGroupExists is null ? null : created.ToList();
    }
}