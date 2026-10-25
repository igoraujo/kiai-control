using KiaiControl.Contracts.Billing;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using CoreBilling = KiaiControl.Core.Entities.Billing;

namespace KiaiControl.UseCases.Billing;

public sealed class BillingCatalogUseCase(IBillingCatalogRepository repository)
{
    public async Task<BillingPlanResponse> CreatePlanAsync(
        Guid organizationId,
        CreateBillingPlanRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePlan(organizationId, request.Name, request.MonthlyAmount, request.DueDay, request.Description);
        var plan = await repository.CreatePlanAsync(new BillingPlan
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            MonthlyAmount = request.MonthlyAmount,
            DueDay = request.DueDay,
            IsActive = true
        }, cancellationToken);
        return ToResponse(plan);
    }

    public async Task<IReadOnlyList<BillingPlanResponse>> ListPlansAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        var plans = await repository.ListPlansAsync(organizationId, page, pageSize, cancellationToken);
        return plans.Select(ToResponse).ToList();
    }

    public async Task<BillingPlanResponse?> UpdatePlanAsync(
        Guid organizationId,
        Guid id,
        UpdateBillingPlanRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePlan(organizationId, request.Name, request.MonthlyAmount, request.DueDay, request.Description);
        var plan = await repository.UpdatePlanAsync(new BillingPlan
        {
            Id = id,
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            MonthlyAmount = request.MonthlyAmount,
            DueDay = request.DueDay,
            IsActive = request.IsActive
        }, cancellationToken);
        return plan is null ? null : ToResponse(plan);
    }

    public Task<bool> DeactivatePlanAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || id == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and PlanId are required.");
        }

        return repository.DeactivatePlanAsync(organizationId, id, cancellationToken);
    }

    public async Task<ClientPlanSubscriptionResponse?> CreateSubscriptionAsync(
        Guid organizationId,
        Guid clientId,
        CreateClientSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || clientId == Guid.Empty || request.PlanId == Guid.Empty || request.StartsAt == default)
        {
            throw new ArgumentException("OrganizationId, ClientId, PlanId and StartsAt are required.");
        }

        var subscription = await repository.CreateSubscriptionAsync(new ClientPlanSubscription
        {
            OrganizationId = organizationId,
            ClientId = clientId,
            PlanId = request.PlanId,
            StartsAt = request.StartsAt,
            IsActive = true
        }, cancellationToken);
        return subscription is null ? null : ToResponse(subscription);
    }

    public async Task<ClientPlanSubscriptionResponse?> CancelSubscriptionAsync(
        Guid organizationId,
        Guid id,
        CancelClientSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || id == Guid.Empty || request.EndsAt == default)
        {
            throw new ArgumentException("OrganizationId, SubscriptionId and EndsAt are required.");
        }

        var subscription = await repository.CancelSubscriptionAsync(organizationId, id, request.EndsAt, cancellationToken);
        return subscription is null ? null : ToResponse(subscription);
    }

    public async Task<IReadOnlyList<BillingResponse>> GenerateInvoicesAsync(
        Guid organizationId,
        GenerateInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (request.ReferenceMonth == default || request.ReferenceMonth.Day != 1)
        {
            throw new ArgumentException("ReferenceMonth must be the first day of a month.");
        }

        var invoices = await repository.GenerateInvoicesAsync(organizationId, request.ReferenceMonth, cancellationToken);
        return invoices?.Select(ToResponse).ToList() ?? [];
    }

    private static void ValidatePlan(Guid organizationId, string name, decimal monthlyAmount, int dueDay, string? description)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            throw new ArgumentException("Name is required and must not exceed 120 characters.");
        }

        if (monthlyAmount < 0 || dueDay is < 1 or > 28)
        {
            throw new ArgumentException("MonthlyAmount must be non-negative and DueDay must be between 1 and 28.");
        }

        if (description?.Length > 255)
        {
            throw new ArgumentException("Description must not exceed 255 characters.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static BillingPlanResponse ToResponse(BillingPlan plan)
    {
        return new BillingPlanResponse
        {
            Id = plan.Id,
            OrganizationId = plan.OrganizationId,
            Name = plan.Name,
            Description = plan.Description,
            MonthlyAmount = plan.MonthlyAmount,
            DueDay = plan.DueDay,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    private static ClientPlanSubscriptionResponse ToResponse(ClientPlanSubscription subscription)
    {
        return new ClientPlanSubscriptionResponse
        {
            Id = subscription.Id,
            OrganizationId = subscription.OrganizationId,
            ClientId = subscription.ClientId,
            PlanId = subscription.PlanId,
            StartsAt = subscription.StartsAt,
            EndsAt = subscription.EndsAt,
            IsActive = subscription.IsActive,
            CreatedAt = subscription.CreatedAt
        };
    }

    private static BillingResponse ToResponse(CoreBilling invoice)
    {
        return new BillingResponse
        {
            Id = invoice.Id,
            OrganizationId = invoice.OrganizationId,
            ClientPlanSubscriptionId = invoice.ClientPlanSubscriptionId,
            ClientId = invoice.ClientId,
            DueDate = invoice.DueDate,
            Amount = invoice.Amount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }
}