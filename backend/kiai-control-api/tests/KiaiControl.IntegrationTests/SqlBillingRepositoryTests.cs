using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class SqlBillingRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ClientFilteredHistory_ShouldOnlyReturnTheRequestedClientRecords()
    {
        var organizationId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "insert into organizations (id, name, status) values (@id, @name, 'active')",
                connection);
            command.Parameters.AddWithValue("id", organizationId);
            command.Parameters.AddWithValue("name", $"Billing history {organizationId:N}");
            await command.ExecuteNonQueryAsync();
        }

        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        using var provider = services.BuildServiceProvider();
        var clients = provider.GetRequiredService<IClientRepository>();
        var catalog = provider.GetRequiredService<IBillingCatalogRepository>();
        var billing = provider.GetRequiredService<IBillingRepository>();

        var firstClient = await clients.CreateAsync(new Client
        {
            OrganizationId = organizationId,
            FullName = "History Client One",
            Status = "active"
        });
        var secondClient = await clients.CreateAsync(new Client
        {
            OrganizationId = organizationId,
            FullName = "History Client Two",
            Status = "active"
        });
        var plan = await catalog.CreatePlanAsync(new BillingPlan
        {
            OrganizationId = organizationId,
            Name = "History plan",
            MonthlyAmount = 100,
            DueDay = 5
        });
        var firstSubscription = await catalog.CreateSubscriptionAsync(new ClientPlanSubscription
        {
            OrganizationId = organizationId,
            ClientId = firstClient.Id,
            PlanId = plan.Id,
            StartsAt = new DateOnly(2026, 10, 1),
            IsActive = true
        });
        var secondSubscription = await catalog.CreateSubscriptionAsync(new ClientPlanSubscription
        {
            OrganizationId = organizationId,
            ClientId = secondClient.Id,
            PlanId = plan.Id,
            StartsAt = new DateOnly(2026, 10, 1),
            IsActive = true
        });
        Assert.NotNull(firstSubscription);
        Assert.NotNull(secondSubscription);

        var firstInvoice = await billing.CreateAsync(new Billing
        {
            OrganizationId = organizationId,
            ClientPlanSubscriptionId = firstSubscription.Id,
            ClientId = firstClient.Id,
            DueDate = new DateOnly(2026, 10, 5),
            Amount = 100,
            Status = "pending"
        });
        var secondInvoice = await billing.CreateAsync(new Billing
        {
            OrganizationId = organizationId,
            ClientPlanSubscriptionId = secondSubscription.Id,
            ClientId = secondClient.Id,
            DueDate = new DateOnly(2026, 10, 5),
            Amount = 100,
            Status = "pending"
        });

        await billing.RegisterPaymentAsync(organizationId, firstInvoice.Id, new Payment
        {
            Amount = 25,
            Method = "pix",
            PaidAt = DateTimeOffset.UtcNow
        });
        await billing.RegisterPaymentAsync(organizationId, secondInvoice.Id, new Payment
        {
            Amount = 25,
            Method = "pix",
            PaidAt = DateTimeOffset.UtcNow
        });

        var subscriptions = await catalog.ListSubscriptionsAsync(
            organizationId, 1, 200, clientId: firstClient.Id);
        var invoices = await billing.ListByOrganizationAsync(
            organizationId, 1, 200, clientId: firstClient.Id);
        var payments = await billing.ListPaymentsByOrganizationAsync(
            organizationId, 1, 200, clientId: firstClient.Id);
        var allSubscriptions = await catalog.ListSubscriptionsAsync(organizationId, 1, 200);
        var allInvoices = await billing.ListByOrganizationAsync(organizationId, 1, 200);
        var allPayments = await billing.ListPaymentsByOrganizationAsync(organizationId, 1, 200);

        Assert.Equal(firstClient.Id, Assert.Single(subscriptions).ClientId);
        Assert.Equal(firstClient.Id, Assert.Single(invoices).ClientId);
        Assert.Equal(firstInvoice.Id, Assert.Single(payments).InvoiceId);
        Assert.Equal(2, allSubscriptions.Count);
        Assert.Equal(2, allInvoices.Count);
        Assert.Equal(2, allPayments.Count);
    }
}
