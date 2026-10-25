using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IBillingRepository
{
    Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Billing>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Billing?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Billing?> UpdateAsync(Billing billing, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Payment?> RegisterPaymentAsync(
        Guid organizationId,
        Guid invoiceId,
        Payment payment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListPaymentsByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListPaymentsByInvoiceAsync(
        Guid organizationId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
