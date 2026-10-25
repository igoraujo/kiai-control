using KiaiControl.Contracts.Billing;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Billing;

namespace KiaiControl.UnitTests;

public sealed class BillingPaymentUseCaseTests
{
    [Fact]
    public async Task RegisterPaymentAsync_ShouldRejectPaymentMethodsOutsideSchemaEnum()
    {
        var useCase = new BillingUseCase(new InMemoryBillingRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.RegisterPaymentAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RegisterPaymentRequest
            {
                Amount = 10,
                Method = "crypto",
                PaidAt = DateTimeOffset.UtcNow
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task RegisterPaymentAsync_ShouldUpdateInvoiceToPaidWhenBalanceIsSettled()
    {
        var repository = new InMemoryBillingRepository();
        var useCase = new BillingUseCase(repository);
        var invoice = await useCase.CreateAsync(new CreateBillingRequest
        {
            OrganizationId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            ClientPlanSubscriptionId = Guid.NewGuid(),
            DueDate = new DateOnly(2026, 10, 10),
            Amount = 100
        }, CancellationToken.None);

        var payment = await useCase.RegisterPaymentAsync(invoice.OrganizationId, invoice.Id, new RegisterPaymentRequest
        {
            Amount = 100,
            Method = "pix",
            PaidAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);
        var updatedInvoice = await useCase.GetByIdAsync(invoice.OrganizationId, invoice.Id, CancellationToken.None);

        Assert.NotNull(payment);
        Assert.Equal("paid", updatedInvoice?.Status);
    }
}