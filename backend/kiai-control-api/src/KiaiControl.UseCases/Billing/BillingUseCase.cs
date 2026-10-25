using KiaiControl.Contracts.Billing;
using KiaiControl.Core.Interfaces;
using CoreBilling = KiaiControl.Core.Entities.Billing;

namespace KiaiControl.UseCases.Billing;

public sealed class BillingUseCase(IBillingRepository billingRepository)
{
    private static readonly string[] AllowedInvoiceStatuses = ["pending", "paid", "overdue", "canceled", "exempt", "partially_paid"];
    private static readonly string[] AllowedPaymentMethods = ["cash", "pix", "bank_transfer", "credit_card", "debit_card", "boleto", "manual_adjustment"];

    public async Task<BillingResponse> CreateAsync(CreateBillingRequest request, CancellationToken cancellationToken)
    {
        Validate(request.OrganizationId, request.ClientId, request.ClientPlanSubscriptionId, request.Amount, request.Status);

        var billing = new CoreBilling
        {
            OrganizationId = request.OrganizationId,
            ClientPlanSubscriptionId = request.ClientPlanSubscriptionId,
            ClientId = request.ClientId,
            DueDate = request.DueDate,
            Amount = request.Amount,
            Status = request.Status
        };

        var created = await billingRepository.CreateAsync(billing, cancellationToken);

        return new BillingResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            ClientPlanSubscriptionId = created.ClientPlanSubscriptionId,
            ClientId = created.ClientId,
            DueDate = created.DueDate,
            Amount = created.Amount,
            Status = created.Status,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<BillingResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var billingItems = await billingRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);

        return billingItems.Select(item => new BillingResponse
        {
            Id = item.Id,
            OrganizationId = item.OrganizationId,
            ClientPlanSubscriptionId = item.ClientPlanSubscriptionId,
            ClientId = item.ClientId,
            DueDate = item.DueDate,
            Amount = item.Amount,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        }).ToList();
    }

    public async Task<BillingResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var billing = await billingRepository.GetByIdAsync(organizationId, id, cancellationToken);
        if (billing is null)
        {
            return null;
        }

        return new BillingResponse
        {
            Id = billing.Id,
            OrganizationId = billing.OrganizationId,
            ClientPlanSubscriptionId = billing.ClientPlanSubscriptionId,
            ClientId = billing.ClientId,
            DueDate = billing.DueDate,
            Amount = billing.Amount,
            Status = billing.Status,
            CreatedAt = billing.CreatedAt,
            UpdatedAt = billing.UpdatedAt
        };
    }

    public async Task<BillingResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateBillingRequest request, CancellationToken cancellationToken)
    {
        Validate(organizationId, request.ClientId, request.ClientPlanSubscriptionId, request.Amount, request.Status);

        var billing = new CoreBilling
        {
            Id = id,
            OrganizationId = organizationId,
            ClientPlanSubscriptionId = request.ClientPlanSubscriptionId,
            ClientId = request.ClientId,
            DueDate = request.DueDate,
            Amount = request.Amount,
            Status = request.Status
        };

        var updated = await billingRepository.UpdateAsync(billing, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        return new BillingResponse
        {
            Id = updated.Id,
            OrganizationId = updated.OrganizationId,
            ClientPlanSubscriptionId = updated.ClientPlanSubscriptionId,
            ClientId = updated.ClientId,
            DueDate = updated.DueDate,
            Amount = updated.Amount,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return billingRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    public async Task<PaymentResponse?> RegisterPaymentAsync(
        Guid organizationId,
        Guid invoiceId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || invoiceId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and InvoiceId are required.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        var method = AllowedPaymentMethods.FirstOrDefault(allowed =>
            string.Equals(allowed, request.Method.Trim(), StringComparison.OrdinalIgnoreCase));
        if (method is null)
        {
            throw new ArgumentException("Payment method is not supported.");
        }

        if (request.PaidAt == default || request.ExternalReference?.Length > 120 || request.Notes?.Length > 255)
        {
            throw new ArgumentException("PaidAt is required; external reference and notes exceed their allowed lengths.");
        }

        var payment = await billingRepository.RegisterPaymentAsync(organizationId, invoiceId, new Core.Entities.Payment
        {
            Amount = request.Amount,
            Method = method,
            PaidAt = request.PaidAt,
            ExternalReference = string.IsNullOrWhiteSpace(request.ExternalReference) ? null : request.ExternalReference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        }, cancellationToken);

        return payment is null ? null : ToResponse(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>> ListPaymentsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var payments = await billingRepository.ListPaymentsByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return payments.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PaymentResponse>> ListPaymentsByInvoiceAsync(
        Guid organizationId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || invoiceId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and InvoiceId are required.");
        }

        var payments = await billingRepository.ListPaymentsByInvoiceAsync(organizationId, invoiceId, cancellationToken);
        return payments.Select(ToResponse).ToList();
    }

    private static void Validate(Guid organizationId, Guid clientId, Guid clientPlanSubscriptionId, decimal amount, string status)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("ClientId is required.");
        }

        if (clientPlanSubscriptionId == Guid.Empty)
        {
            throw new ArgumentException("ClientPlanSubscriptionId is required.");
        }

        if (amount < 0)
        {
            throw new ArgumentException("Amount must be greater than or equal to zero.");
        }

        if (!AllowedInvoiceStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Status is not a supported invoice status.");
        }
    }

    private static PaymentResponse ToResponse(Core.Entities.Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            OrganizationId = payment.OrganizationId,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            Method = payment.Method,
            PaidAt = payment.PaidAt,
            ExternalReference = payment.ExternalReference,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }
}
