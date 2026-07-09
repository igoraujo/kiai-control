using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IClientRepository
{
    Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
