using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.ClassGroups;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlClassGroupRepository(IDbConnectionFactory dbConnectionFactory) : IClassGroupRepository
{
    public async Task<ClassGroup?> CreateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default)
    {
        classGroup.Id = Guid.NewGuid();
        classGroup.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClassGroup>(ClassGroupQueries.Insert, classGroup);
    }

    public async Task<IReadOnlyList<ClassGroup>> ListByOrganizationAsync(
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
        var classGroups = await connection.QueryAsync<ClassGroup>(ClassGroupQueries.ListByOrganization, parameters);
        return classGroups.ToList();
    }

    public async Task<ClassGroup?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClassGroup>(ClassGroupQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<ClassGroup?> UpdateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default)
    {
        classGroup.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClassGroup>(ClassGroupQueries.Update, classGroup);
    }

    public async Task<bool> ArchiveAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ClassGroupQueries.Archive,
            new { OrganizationId = organizationId, Id = id, UpdatedAt = DateTimeOffset.UtcNow });
        return affected > 0;
    }

    public async Task<ClassSchedule?> CreateScheduleAsync(
        Guid organizationId,
        ClassSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        schedule.Id = Guid.NewGuid();
        schedule.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleOrDefaultAsync<ClassScheduleRecord>(
            ClassGroupQueries.CreateSchedule,
            new
            {
                OrganizationId = organizationId,
                schedule.Id,
                schedule.ClassGroupId,
                schedule.DayOfWeek,
                StartsAt = schedule.StartsAt.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                EndsAt = schedule.EndsAt.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                schedule.CreatedAt
            });
        if (created is null)
        {
            return null;
        }

        return new ClassSchedule
        {
            Id = created.Id,
            ClassGroupId = created.ClassGroupId,
            DayOfWeek = created.DayOfWeek,
            StartsAt = TimeOnly.FromTimeSpan(created.StartsAt),
            EndsAt = TimeOnly.FromTimeSpan(created.EndsAt),
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<bool> DeleteScheduleAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ClassGroupQueries.DeleteSchedule,
            new { OrganizationId = organizationId, ClassGroupId = classGroupId, ScheduleId = scheduleId });
        return affected > 0;
    }

    public async Task<IReadOnlyList<ClassScheduleRecord>> ListSchedulesAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<ClassScheduleRecord>(ClassGroupQueries.ListSchedules, new
        {
            OrganizationId = organizationId,
            ClassGroupId = classGroupId
        });
        return items.ToList();
    }

    public async Task<Enrollment?> CreateEnrollmentAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var capacity = await connection.QuerySingleOrDefaultAsync<int?>(
            ClassGroupQueries.LockEnrollmentTarget,
            new { enrollment.OrganizationId, enrollment.ClassGroupId },
            transaction);
        if (capacity is null)
        {
            return null;
        }

        var otherActiveEnrollments = await connection.QuerySingleAsync<int>(
            ClassGroupQueries.CountOtherActiveEnrollments,
            new { enrollment.OrganizationId, enrollment.ClassGroupId, enrollment.ClientId },
            transaction);
        if (otherActiveEnrollments >= capacity.Value)
        {
            return null;
        }

        enrollment.Id = Guid.NewGuid();
        enrollment.EnrolledAt = DateTimeOffset.UtcNow;
        var created = await connection.QuerySingleOrDefaultAsync<Enrollment>(
            ClassGroupQueries.CreateEnrollment,
            new
            {
                enrollment.Id,
                enrollment.OrganizationId,
                enrollment.ClassGroupId,
                enrollment.ClientId,
                enrollment.EnrolledAt
            },
            transaction);
        if (created is null)
        {
            return null;
        }

        transaction.Commit();
        return created;
    }

    public async Task<bool> CancelEnrollmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ClassGroupQueries.CancelEnrollment,
            new
            {
                OrganizationId = organizationId,
                ClassGroupId = classGroupId,
                EnrollmentId = enrollmentId,
                EndedAt = DateTimeOffset.UtcNow
            });
        return affected > 0;
    }

    public async Task<IReadOnlyList<Enrollment>> ListEnrollmentsAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<Enrollment>(ClassGroupQueries.ListEnrollments, new
        {
            OrganizationId = organizationId,
            ClassGroupId = classGroupId
        });
        return items.ToList();
    }

    public async Task<bool> AssignTeacherAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        bool isPrimary,
        DateOnly startsAt,
        DateOnly? endsAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ClassGroupQueries.AssignTeacher,
            new
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ClassGroupId = classGroupId,
                TeacherId = teacherId,
                IsPrimary = isPrimary,
                StartsAt = startsAt.ToDateTime(TimeOnly.MinValue),
                EndsAt = endsAt?.ToDateTime(TimeOnly.MinValue)
            });
        return affected > 0;
    }

    public async Task<IReadOnlyList<ClassGroupTeacherAssignment>> ListTeacherAssignmentsAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<ClassGroupTeacherAssignment>(ClassGroupQueries.ListTeacherAssignments, new
        {
            OrganizationId = organizationId,
            ClassGroupId = classGroupId
        });
        return items.ToList();
    }

    public async Task<bool> RemoveTeacherAssignmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        DateOnly startsAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ClassGroupQueries.RemoveTeacherAssignment,
            new
            {
                OrganizationId = organizationId,
                ClassGroupId = classGroupId,
                TeacherId = teacherId,
                StartsAt = startsAt.ToDateTime(TimeOnly.MinValue)
            });
        return affected > 0;
    }
}