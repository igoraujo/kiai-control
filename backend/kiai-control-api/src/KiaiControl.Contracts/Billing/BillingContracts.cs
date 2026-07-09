namespace KiaiControl.Contracts.Billing;

public sealed class CreateBillingRequest
{
    public Guid OrganizationId { get; set; }

    public Guid ClientId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending";
}

public sealed class BillingResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientId { get; init; }

    public decimal Amount { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
