namespace KiaiControl.Contracts.Billing;

public sealed class CreateBillingPlanRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal MonthlyAmount { get; set; }

    public int DueDay { get; set; }
}

public sealed class UpdateBillingPlanRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal MonthlyAmount { get; set; }

    public int DueDay { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class BillingPlanResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal MonthlyAmount { get; init; }

    public int DueDay { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateClientSubscriptionRequest
{
    public Guid PlanId { get; set; }

    public DateOnly StartsAt { get; set; }
}

public sealed class ClientPlanSubscriptionResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClientId { get; init; }

    public Guid PlanId { get; init; }

    public DateOnly StartsAt { get; init; }

    public DateOnly? EndsAt { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CancelClientSubscriptionRequest
{
    public DateOnly EndsAt { get; set; }
}

public sealed class GenerateInvoicesRequest
{
    public DateOnly ReferenceMonth { get; set; }
}