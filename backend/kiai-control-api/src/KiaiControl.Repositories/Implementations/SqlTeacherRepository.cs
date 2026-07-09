using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Teachers;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlTeacherRepository(IDbConnectionFactory dbConnectionFactory) : ITeacherRepository
{
    public async Task<Teacher> CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.Id = Guid.NewGuid();
        teacher.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<Teacher>(TeacherQueries.Insert, teacher);
        return created;
    }

    public async Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
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
        var items = await connection.QueryAsync<Teacher>(TeacherQueries.ListByOrganization, parameters);
        return items.ToList();
    }

    public async Task<Teacher?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Teacher>(TeacherQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Teacher?> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Teacher>(TeacherQueries.Update, teacher);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(TeacherQueries.Delete, new { OrganizationId = organizationId, Id = id });
        return affected > 0;
    }
}
