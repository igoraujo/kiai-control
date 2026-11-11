using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Catalog;

namespace KiaiControl.Repositories.Implementations;
public sealed class SqlCatalogRepository(IDbConnectionFactory factory) : ICatalogRepository
{
    public async Task<PagedResult<Lesson>> LessonsAsync(Guid organizationId, int page, int pageSize, DateTimeOffset? from, DateTimeOffset? to, string? status, Guid? teacherId, CancellationToken ct, string area = "shared")
    {
        using var c = await factory.OpenConnectionAsync(ct);
        using var r = await c.QueryMultipleAsync(new CommandDefinition(CatalogQueries.Lessons, new { OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize, From = from?.ToUniversalTime(), To = to?.ToUniversalTime(), Status = string.IsNullOrWhiteSpace(status) ? null : status, TeacherId = teacherId, Area = area }, cancellationToken: ct));
        return new((await r.ReadAsync<Lesson>()).ToArray(), await r.ReadSingleAsync<int>());
    }

    public Task<PagedResult<Client>> ClientsAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared") => ReadAsync<Client>(CatalogQueries.Clients, organizationId, page, pageSize, search, cancellationToken, area);
    public Task<PagedResult<Teacher>> TeachersAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared") => ReadAsync<Teacher>(CatalogQueries.Teachers, organizationId, page, pageSize, search, cancellationToken, area);
    public Task<PagedResult<Branch>> BranchesAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken) => ReadAsync<Branch>(CatalogQueries.Branches, organizationId, page, pageSize, search, cancellationToken);
    public Task<PagedResult<Modality>> ModalitiesAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared") => ReadAsync<Modality>(CatalogQueries.Modalities, organizationId, page, pageSize, search, cancellationToken, area);
    private async Task<PagedResult<T>> ReadAsync<T>(string sql, Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared")
    {
        using var connection = await factory.OpenConnectionAsync(cancellationToken);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { OrganizationId = organizationId, Offset = (page - 1) * pageSize, PageSize = pageSize, Area = area, Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%" }, cancellationToken: cancellationToken));
        return new((await result.ReadAsync<T>()).ToList(), await result.ReadSingleAsync<int>());
    }
}
