namespace KiaiControl.Core.Entities;

public sealed class Billing
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClientPlanSubscriptionId { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
