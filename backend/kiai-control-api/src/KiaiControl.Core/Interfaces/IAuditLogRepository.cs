using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IAuditLogRepository
{
    Task RecordAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}