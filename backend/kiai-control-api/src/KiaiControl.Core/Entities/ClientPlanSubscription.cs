namespace KiaiControl.Core.Entities;

public sealed class ClientPlanSubscription
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClientId { get; set; }

    public Guid PlanId { get; set; }

    public DateOnly StartsAt { get; set; }

    public DateOnly? EndsAt { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}