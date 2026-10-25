using KiaiControl.Contracts.Branches;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Branches;

namespace KiaiControl.UnitTests;

public sealed class BranchUseCaseTests
{
    [Fact]
    public async Task CreateAsync_ShouldNormalizeBranchFields()
    {
        var useCase = new BranchUseCase(new FakeBranchRepository());

        var branch = await useCase.CreateAsync(Guid.NewGuid(), new CreateBranchRequest
        {
            Name = "  Centro  ",
            City = "  Sao Paulo  ",
            State = " SP "
        }, CancellationToken.None);

        Assert.NotNull(branch);
        Assert.Equal("Centro", branch.Name);
        Assert.Equal("Sao Paulo", branch.City);
        Assert.Equal("SP", branch.State);
    }

    private sealed class FakeBranchRepository : IBranchRepository
    {
        public Task<Branch?> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            branch.Id = Guid.NewGuid();
            branch.CreatedAt = DateTimeOffset.UtcNow;
            return Task.FromResult<Branch?>(branch);
        }

        public Task<IReadOnlyList<Branch>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Branch>>([]);
        }

        public Task<Branch?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Branch?>(null);
        }

        public Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Branch?>(null);
        }

        public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}