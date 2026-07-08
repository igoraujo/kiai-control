using KiaiControl.Contracts.Billing;
using KiaiControl.Core.Interfaces;
using CoreBilling = KiaiControl.Core.Entities.Billing;

namespace KiaiControl.UseCases.Billing;

public sealed class BillingUseCase(IBillingRepository billingRepository)
{
    public async Task<BillingResponse> CreateAsync(CreateBillingRequest request, CancellationToken cancellationToken)
    {
        var billing = new CoreBilling
        {
            OrganizationId = request.OrganizationId,
            StudentId = request.StudentId,
            Amount = request.Amount,
            Status = request.Status
        };

        var created = await billingRepository.CreateAsync(billing, cancellationToken);

        return new BillingResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            StudentId = created.StudentId,
            Amount = created.Amount,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IReadOnlyList<BillingResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var billingItems = await billingRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        return billingItems.Select(item => new BillingResponse
        {
            Id = item.Id,
            OrganizationId = item.OrganizationId,
            StudentId = item.StudentId,
            Amount = item.Amount,
            Status = item.Status,
            CreatedAt = item.CreatedAt
        }).ToList();
    }
}
