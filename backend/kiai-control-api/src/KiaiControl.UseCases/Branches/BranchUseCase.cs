using KiaiControl.Contracts.Branches;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Branches;

public sealed class BranchUseCase(IBranchRepository branchRepository)
{
    public async Task<BranchResponse?> CreateAsync(
        Guid organizationId,
        CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.Name, request.AddressLine, request.City, request.State, request.PostalCode);
        var branch = await branchRepository.CreateAsync(new Branch
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            AddressLine = Normalize(request.AddressLine),
            City = Normalize(request.City),
            State = Normalize(request.State),
            PostalCode = Normalize(request.PostalCode)
        }, cancellationToken);
        return branch is null ? null : ToResponse(branch);
    }

    public async Task<IReadOnlyList<BranchResponse>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var branches = await branchRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return branches.Select(ToResponse).ToList();
    }

    public async Task<BranchResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var branch = await branchRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return branch is null ? null : ToResponse(branch);
    }

    public async Task<BranchResponse?> UpdateAsync(
        Guid organizationId,
        Guid id,
        UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.Name, request.AddressLine, request.City, request.State, request.PostalCode);
        var branch = await branchRepository.UpdateAsync(new Branch
        {
            Id = id,
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            AddressLine = Normalize(request.AddressLine),
            City = Normalize(request.City),
            State = Normalize(request.State),
            PostalCode = Normalize(request.PostalCode)
        }, cancellationToken);
        return branch is null ? null : ToResponse(branch);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("BranchId is required.");
        }

        return branchRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    private static void Validate(
        Guid organizationId,
        string name,
        string? addressLine,
        string? city,
        string? state,
        string? postalCode)
    {
        EnsureOrganization(organizationId);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            throw new ArgumentException("Name is required and must not exceed 160 characters.");
        }

        if (addressLine?.Length > 255 || city?.Length > 100 || state?.Length > 50 || postalCode?.Length > 20)
        {
            throw new ArgumentException("One or more branch fields exceed their maximum length.");
        }
    }

    private static void EnsureOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static BranchResponse ToResponse(Branch branch)
    {
        return new BranchResponse
        {
            Id = branch.Id,
            OrganizationId = branch.OrganizationId,
            Name = branch.Name,
            AddressLine = branch.AddressLine,
            City = branch.City,
            State = branch.State,
            PostalCode = branch.PostalCode,
            CreatedAt = branch.CreatedAt,
            UpdatedAt = branch.UpdatedAt
        };
    }
}