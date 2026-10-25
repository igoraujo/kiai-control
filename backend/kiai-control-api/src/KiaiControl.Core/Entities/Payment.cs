namespace KiaiControl.Core.Entities;

public sealed class Payment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = string.Empty;

    public DateTimeOffset PaidAt { get; set; }

    public string? ExternalReference { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}