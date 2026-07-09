using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Id = Guid.NewGuid();
        client.CreatedAt = DateTimeOffset.UtcNow;
        _clients.Add(client);
        return Task.FromResult(client);
    }

    public Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = _clients.Where(client => client.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<Client>>(items);
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = _clients.FirstOrDefault(item => item.Id == id);
        return Task.FromResult(client);
    }
}
