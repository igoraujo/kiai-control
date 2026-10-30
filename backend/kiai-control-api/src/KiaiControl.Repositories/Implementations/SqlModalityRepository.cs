using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Modalities;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlModalityRepository(IDbConnectionFactory dbConnectionFactory) : IModalityRepository
{
    public async Task<Modality> CreateAsync(Modality modality, CancellationToken cancellationToken = default)
    {
        modality.Id = Guid.NewGuid();
        modality.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Modality>(ModalityQueries.Insert, modality);
    }

    public async Task<IReadOnlyList<Modality>> ListByOrganizationAsync(
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
        var modalities = await connection.QueryAsync<Modality>(ModalityQueries.ListByOrganization, parameters);
        return modalities.ToList();
    }

    public async Task<Modality?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Modality>(ModalityQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Modality?> UpdateAsync(Modality modality, CancellationToken cancellationToken = default)
    {
        modality.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Modality>(ModalityQueries.Update, modality);
    }

    public async Task<bool> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ModalityQueries.Deactivate,
            new { OrganizationId = organizationId, Id = id, UpdatedAt = DateTimeOffset.UtcNow });
        return affected > 0;
    }

    public async Task<IReadOnlyList<Modality>> ListClientModalitiesAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var modalities = await connection.QueryAsync<Modality>(ModalityQueries.ListClientModalities, new
        {
            OrganizationId = organizationId,
            ClientId = clientId
        });
        return modalities.ToList();
    }

    public async Task<bool> LinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ModalityQueries.LinkClient,
            new { OrganizationId = organizationId, ClientId = clientId, ModalityId = modalityId });
        return affected > 0;
    }

    public async Task<bool> UnlinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ModalityQueries.UnlinkClient,
            new { OrganizationId = organizationId, ClientId = clientId, ModalityId = modalityId });
        return affected > 0;
    }
}