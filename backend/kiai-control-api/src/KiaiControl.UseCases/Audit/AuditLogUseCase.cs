using KiaiControl.Contracts.Audit;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Audit;

public sealed class AuditLogUseCase(IAuditLogRepository auditLogRepository)
{
    public async Task<IReadOnlyList<AuditLogResponse>> ListAsync(
        Guid organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (from is not null && to is not null && to <= from)
        {
            throw new ArgumentException("To must be later than From.");
        }

        if (from is not null && to is not null && to.Value - from.Value > TimeSpan.FromDays(366))
        {
            throw new ArgumentException("Audit query range cannot exceed 366 days.");
        }

        var logs = await auditLogRepository.ListAsync(organizationId, from, to, page, pageSize, cancellationToken);
        return logs.Select(log => new AuditLogResponse
        {
            Id = log.Id,
            OrganizationId = log.OrganizationId,
            ActorPersonId = log.ActorPersonId,
            ActorUserAccountId = log.ActorUserAccountId,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Metadata = log.Metadata,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            CreatedAt = log.CreatedAt
        }).ToList();
    }
}