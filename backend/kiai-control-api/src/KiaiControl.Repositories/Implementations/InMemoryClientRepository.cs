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

    public Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _clients
            .Where(client => client.OrganizationId == organizationId)
            .OrderByDescending(client => client.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Client>>(items);
    }

    public Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var client = _clients.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(client);
    }

    public Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        var index = _clients.FindIndex(item => item.OrganizationId == client.OrganizationId && item.Id == client.Id);
        if (index < 0)
        {
            return Task.FromResult<Client?>(null);
        }

        var current = _clients[index];
        current.FullName = client.FullName;
        current.Status = client.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Client?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _clients.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}
