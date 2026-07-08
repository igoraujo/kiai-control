using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IBillingRepository
{
    Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
