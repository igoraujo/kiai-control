using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Branches;
using Npgsql;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlBranchRepository(IDbConnectionFactory dbConnectionFactory) : IBranchRepository
{
    public async Task<Branch?> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        branch.Id = Guid.NewGuid();
        branch.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Branch>(BranchQueries.Insert, branch);
    }

    public async Task<IReadOnlyList<Branch>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var branches = await connection.QueryAsync<Branch>(BranchQueries.ListByOrganization, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return branches.ToList();
    }

    public async Task<Branch?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Branch>(BranchQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Branch>(BranchQueries.Update, branch);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        try
        {
            var affected = await connection.ExecuteAsync(BranchQueries.Delete, new { OrganizationId = organizationId, Id = id });
            return affected > 0;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return false;
        }
    }
}