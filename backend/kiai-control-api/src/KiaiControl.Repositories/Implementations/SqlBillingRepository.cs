using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Billing;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlBillingRepository(IDbConnectionFactory dbConnectionFactory) : IBillingRepository
{
    public async Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.Id = Guid.NewGuid();
        billing.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleAsync<Billing>(BillingQueries.Insert, billing);
        return created;
    }

    public async Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var parameters = new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        };

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<Billing>(BillingQueries.ListByOrganization, parameters);
        return items.ToList();
    }

    public async Task<Billing?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Billing>(BillingQueries.GetById, new { OrganizationId = organizationId, Id = id });
    }

    public async Task<Billing?> UpdateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Billing>(BillingQueries.Update, billing);
    }

    public async Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(BillingQueries.Delete, new { OrganizationId = organizationId, Id = id });
        return affected > 0;
    }

    public async Task<Payment?> RegisterPaymentAsync(
        Guid organizationId,
        Guid invoiceId,
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var invoice = await connection.QuerySingleOrDefaultAsync<InvoicePaymentSnapshot>(
            BillingQueries.LockInvoiceForPayment,
            new { OrganizationId = organizationId, InvoiceId = invoiceId },
            transaction);
        if (invoice is null || invoice.Status is "canceled" or "exempt")
        {
            return null;
        }

        var outstanding = invoice.Amount - invoice.DiscountAmount + invoice.FineAmount + invoice.InterestAmount - invoice.PaidAmount;
        if (payment.Amount > outstanding)
        {
            return null;
        }

        payment.Id = Guid.NewGuid();
        payment.OrganizationId = organizationId;
        payment.InvoiceId = invoiceId;
        payment.CreatedAt = DateTimeOffset.UtcNow;

        var created = await connection.QuerySingleAsync<Payment>(BillingQueries.InsertPayment, payment, transaction);
        await connection.ExecuteAsync(
            BillingQueries.ApplyPaymentToInvoice,
            new
            {
                OrganizationId = organizationId,
                InvoiceId = invoiceId,
                PaymentAmount = payment.Amount,
                payment.PaidAt
            },
            transaction);
        transaction.Commit();
        return created;
    }

    public async Task<IReadOnlyList<Payment>> ListPaymentsByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var payments = await connection.QueryAsync<Payment>(BillingQueries.ListPaymentsByOrganization, new
        {
            OrganizationId = organizationId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return payments.ToList();
    }

    public async Task<IReadOnlyList<Payment>> ListPaymentsByInvoiceAsync(
        Guid organizationId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var payments = await connection.QueryAsync<Payment>(
            BillingQueries.ListPaymentsByInvoice,
            new { OrganizationId = organizationId, InvoiceId = invoiceId });
        return payments.ToList();
    }

    private sealed class InvoicePaymentSnapshot
    {
        public decimal Amount { get; init; }

        public decimal DiscountAmount { get; init; }

        public decimal FineAmount { get; init; }

        public decimal InterestAmount { get; init; }

        public decimal PaidAmount { get; init; }

        public string Status { get; init; } = string.Empty;
    }
}
