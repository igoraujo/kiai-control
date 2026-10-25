using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Audit;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlAuditLogRepository(IDbConnectionFactory dbConnectionFactory) : IAuditLogRepository
{
    public async Task RecordAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        auditLog.Id = auditLog.Id == Guid.Empty ? Guid.NewGuid() : auditLog.Id;
        auditLog.CreatedAt = auditLog.CreatedAt == default ? DateTimeOffset.UtcNow : auditLog.CreatedAt;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(AuditQueries.Insert, auditLog);
    }

    public async Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<AuditLog>(AuditQueries.List, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return items.ToList();
    }
}