using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryBillingRepository : IBillingRepository
{
    private readonly List<Billing> _billing = [];

    public Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.Id = Guid.NewGuid();
        billing.CreatedAt = DateTimeOffset.UtcNow;
        _billing.Add(billing);
        return Task.FromResult(billing);
    }

    public Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = _billing.Where(record => record.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<Billing>>(items);
    }
}
