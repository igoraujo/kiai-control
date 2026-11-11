using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IOrganizationRepository
{
    Task<(Organization Organization, Guid OwnerUserId)?> RegisterAsync(
        Organization organization,
        string ownerFullName,
        string ownerEmail,
        string ownerPasswordHash,
        CancellationToken cancellationToken = default,
        AuditLog? audit = null);

    Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Organization>> ListAsync(CancellationToken cancellationToken = default);

    Task<Organization?> UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
}
