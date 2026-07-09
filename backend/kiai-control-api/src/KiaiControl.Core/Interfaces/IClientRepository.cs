using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IClientRepository
{
    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
}
