using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Clients;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlClientRepository(IDbConnectionFactory dbConnectionFactory) : IClientRepository
{
    public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Id = Guid.NewGuid();
        client.PersonId = Guid.NewGuid();
        client.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<Client>(ClientQueries.Insert, client);
        return created;
    }

    public async Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
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
        var items = await connection.QueryAsync<Client>(ClientQueries.ListByOrganization, parameters);
        return items.ToList();
    }

    public async Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Client>(ClientQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Client>(ClientQueries.Update, client);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(ClientQueries.Delete, new { OrganizationId = organizationId, Id = id });
        return affected > 0;
    }
}
