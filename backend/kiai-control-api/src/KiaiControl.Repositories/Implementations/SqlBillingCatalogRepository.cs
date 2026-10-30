using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Billing;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlBillingCatalogRepository(IDbConnectionFactory dbConnectionFactory) : IBillingCatalogRepository
{
    public async Task<BillingPlan> CreatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default)
    {
        plan.Id = Guid.NewGuid();
        plan.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<BillingPlan>(BillingCatalogQueries.InsertPlan, plan);
    }

    public async Task<IReadOnlyList<BillingPlan>> ListPlansAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var plans = await connection.QueryAsync<BillingPlan>(BillingCatalogQueries.ListPlans, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return plans.ToList();
    }

    public async Task<BillingPlan?> UpdatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default)
    {
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BillingPlan>(BillingCatalogQueries.UpdatePlan, plan);
    }

    public async Task<bool> DeactivatePlanAsync(Guid organizationId, Guid planId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(BillingCatalogQueries.DeactivatePlan, new
        {
            OrganizationId = organizationId,
            PlanId = planId,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        return affected > 0;
    }

    public async Task<ClientPlanSubscription?> CreateSubscriptionAsync(
        ClientPlanSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        subscription.Id = Guid.NewGuid();
        subscription.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClientPlanSubscription>(BillingCatalogQueries.CreateSubscription, subscription);
    }

    public async Task<IReadOnlyList<ClientPlanSubscription>> ListSubscriptionsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var subscriptions = await connection.QueryAsync<ClientPlanSubscription>(BillingCatalogQueries.ListSubscriptions, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return subscriptions.ToList();
    }

    public async Task<ClientPlanSubscription?> CancelSubscriptionAsync(
        Guid organizationId,
        Guid subscriptionId,
        DateOnly endsAt,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClientPlanSubscription>(BillingCatalogQueries.CancelSubscription, new
        {
            OrganizationId = organizationId,
            SubscriptionId = subscriptionId,
            EndsAt = endsAt
        });
    }

    public async Task<IReadOnlyList<Billing>?> GenerateInvoicesAsync(
        Guid organizationId,
        DateOnly referenceMonth,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var invoices = await connection.QueryAsync<Billing>(BillingCatalogQueries.GenerateInvoices, new
        {
            OrganizationId = organizationId,
            ReferenceMonth = referenceMonth
        });
        return invoices.ToList();
    }
}