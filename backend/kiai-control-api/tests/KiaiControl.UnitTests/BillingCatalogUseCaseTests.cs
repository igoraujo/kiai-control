using KiaiControl.Contracts.Billing;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Billing;

namespace KiaiControl.UnitTests;

public sealed class BillingCatalogUseCaseTests
{
    [Fact]
    public async Task CreatePlanAsync_ShouldRejectDueDayOutsideSupportedRange()
    {
        var useCase = new BillingCatalogUseCase(new InMemoryBillingCatalogRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.CreatePlanAsync(Guid.NewGuid(), new CreateBillingPlanRequest
        {
            Name = "Mensal",
            MonthlyAmount = 100,
            DueDay = 29
        }, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateInvoicesAsync_ShouldRequireFirstDayOfMonth()
    {
        var useCase = new BillingCatalogUseCase(new InMemoryBillingCatalogRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.GenerateInvoicesAsync(Guid.NewGuid(), new GenerateInvoicesRequest
        {
            ReferenceMonth = new DateOnly(2026, 10, 2)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateInvoicesAsync_ShouldBeIdempotentForTheSameMonth()
    {
        var useCase = new BillingCatalogUseCase(new InMemoryBillingCatalogRepository());
        var organizationId = Guid.NewGuid();
        var plan = await useCase.CreatePlanAsync(organizationId, new CreateBillingPlanRequest
        {
            Name = "Mensal",
            MonthlyAmount = 100,
            DueDay = 10
        }, CancellationToken.None);
        var subscription = await useCase.CreateSubscriptionAsync(organizationId, Guid.NewGuid(), new CreateClientSubscriptionRequest
        {
            PlanId = plan.Id,
            StartsAt = new DateOnly(2026, 10, 1)
        }, CancellationToken.None);
        Assert.NotNull(subscription);

        var firstRun = await useCase.GenerateInvoicesAsync(organizationId, new GenerateInvoicesRequest
        {
            ReferenceMonth = new DateOnly(2026, 10, 1)
        }, CancellationToken.None);
        var secondRun = await useCase.GenerateInvoicesAsync(organizationId, new GenerateInvoicesRequest
        {
            ReferenceMonth = new DateOnly(2026, 10, 1)
        }, CancellationToken.None);

        Assert.Single(firstRun);
        Assert.Empty(secondRun);
        Assert.Equal(new DateOnly(2026, 10, 10), firstRun[0].DueDate);
    }

    [Fact]
    public async Task ListSubscriptionsAsync_ShouldReturnOnlySubscriptionsForOrganization()
    {
        var useCase = new BillingCatalogUseCase(new InMemoryBillingCatalogRepository());
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var plan = await useCase.CreatePlanAsync(organizationId, new CreateBillingPlanRequest
        {
            Name = "Mensal",
            MonthlyAmount = 100,
            DueDay = 10
        }, CancellationToken.None);
        var otherPlan = await useCase.CreatePlanAsync(otherOrganizationId, new CreateBillingPlanRequest
        {
            Name = "Outro",
            MonthlyAmount = 50,
            DueDay = 5
        }, CancellationToken.None);
        await useCase.CreateSubscriptionAsync(organizationId, Guid.NewGuid(), new CreateClientSubscriptionRequest
        {
            PlanId = plan.Id,
            StartsAt = new DateOnly(2026, 10, 1)
        }, CancellationToken.None);
        await useCase.CreateSubscriptionAsync(otherOrganizationId, Guid.NewGuid(), new CreateClientSubscriptionRequest
        {
            PlanId = otherPlan.Id,
            StartsAt = new DateOnly(2026, 10, 1)
        }, CancellationToken.None);

        var subscriptions = await useCase.ListSubscriptionsAsync(organizationId, 1, 50, CancellationToken.None);

        var subscription = Assert.Single(subscriptions);
        Assert.Equal(organizationId, subscription.OrganizationId);
        Assert.Equal(plan.Id, subscription.PlanId);
    }
}