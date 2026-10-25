namespace KiaiControl.Contracts.Billing;

public sealed class CreateBillingRequest
{
    public Guid OrganizationId { get; set; }

    public Guid ClientPlanSubscriptionId { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending";
}

public sealed class UpdateBillingRequest
{
    public Guid ClientPlanSubscriptionId { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending";
}

public sealed class BillingResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientPlanSubscriptionId { get; init; }

    public Guid ClientId { get; init; }

    public DateOnly DueDate { get; init; }

    public decimal Amount { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class RegisterPaymentRequest
{
    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public DateTimeOffset PaidAt { get; set; }

    public string? ExternalReference { get; set; }

    public string? Notes { get; set; }
}

public sealed class PaymentResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid InvoiceId { get; init; }

    public decimal Amount { get; init; }

    public string Method { get; init; } = string.Empty;

    public DateTimeOffset PaidAt { get; init; }

    public string? ExternalReference { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
