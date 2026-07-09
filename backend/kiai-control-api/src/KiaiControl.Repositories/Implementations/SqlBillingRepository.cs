using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Billing;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlBillingRepository(IDbConnectionFactory dbConnectionFactory) : IBillingRepository
{
    public async Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.Id = Guid.NewGuid();
        billing.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<Billing>(BillingQueries.Insert, billing);
        return created;
    }

    public async Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
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
        var items = await connection.QueryAsync<Billing>(BillingQueries.ListByOrganization, parameters);
        return items.ToList();
    }

    public async Task<Billing?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Billing>(BillingQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Billing?> UpdateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Billing>(BillingQueries.Update, billing);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(BillingQueries.Delete, new { OrganizationId = organizationId, Id = id });
        return affected > 0;
    }
}
