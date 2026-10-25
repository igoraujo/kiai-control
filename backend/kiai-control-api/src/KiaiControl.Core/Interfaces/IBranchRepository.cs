using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IBranchRepository
{
    Task<Branch?> CreateAsync(Branch branch, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Branch?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
}