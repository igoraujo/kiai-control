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

    public Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _billing
            .Where(record => record.OrganizationId == organizationId)
            .OrderByDescending(record => record.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Billing>>(items);
    }

    public Task<Billing?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var billing = _billing.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(billing);
    }

    public Task<Billing?> UpdateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        var index = _billing.FindIndex(item => item.OrganizationId == billing.OrganizationId && item.Id == billing.Id);
        if (index < 0)
        {
            return Task.FromResult<Billing?>(null);
        }

        var current = _billing[index];
        current.ClientPlanSubscriptionId = billing.ClientPlanSubscriptionId;
        current.ClientId = billing.ClientId;
        current.DueDate = billing.DueDate;
        current.Amount = billing.Amount;
        current.Status = billing.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Billing?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _billing.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}
