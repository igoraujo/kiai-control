using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IBillingCatalogRepository
{
    Task<BillingPlan> CreatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingPlan>> ListPlansAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<BillingPlan?> UpdatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default);

    Task<bool> DeactivatePlanAsync(Guid organizationId, Guid planId, CancellationToken cancellationToken = default);

    Task<ClientPlanSubscription?> CreateSubscriptionAsync(ClientPlanSubscription subscription, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientPlanSubscription>> ListSubscriptionsAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ClientPlanSubscription?> CancelSubscriptionAsync(
        Guid organizationId,
        Guid subscriptionId,
        DateOnly endsAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Billing>?> GenerateInvoicesAsync(
        Guid organizationId,
        DateOnly referenceMonth,
        CancellationToken cancellationToken = default);
}