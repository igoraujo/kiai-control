using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IPlatformAdministrationRepository
{
    Task<PagedResult<Organization>> ListOrganizationsAsync(int page, int pageSize, string? search, string? status, CancellationToken ct, string archive = "active");
    Task<Organization?> GetOrganizationAsync(Guid organizationId, CancellationToken ct);
    Task<bool> ChangeArchiveAsync(Guid organizationId, bool archived, string confirmedName, AuditLog audit, CancellationToken ct, bool reactivate = false);
    Task<bool> SetPrivacyHoldAsync(Guid organizationId, bool hold, AuditLog audit, CancellationToken ct);
    Task<AuditLog?> GetAuditAsync(Guid organizationId, Guid auditId, CancellationToken ct);
    Task<bool> UpdateOrganizationAsync(Organization organization, AuditLog audit, CancellationToken ct);
    Task<bool> SetOrganizationStatusAsync(Guid organizationId, string status, AuditLog audit, CancellationToken ct);
    Task<PagedResult<PlatformAccount>> ListAccountsAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken ct);
    Task<bool> CreateAccountAsync(Guid organizationId, string name, string email, string passwordHash, string role, AuditLog audit, CancellationToken ct);
    Task<bool> UpdateAccountAsync(Guid organizationId, Guid userId, string email, string[] roles, bool active, AuditLog audit, CancellationToken ct);
    Task<bool> ResetPasswordAsync(Guid organizationId, Guid userId, string passwordHash, AuditLog audit, CancellationToken ct);
    Task<bool> RevokeSessionsAsync(Guid organizationId, Guid userId, AuditLog audit, CancellationToken ct);
    Task<PagedResult<AuditLog>> ListAuditAsync(PlatformLogFilter filter, CancellationToken ct);
    Task<PagedResult<PlatformIncident>> ListIncidentsAsync(PlatformLogFilter filter, CancellationToken ct);
    Task<PlatformOverview> GetOverviewAsync(CancellationToken ct);
    Task RecordIncidentAsync(PlatformIncident incident, CancellationToken ct);
}
