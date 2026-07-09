using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Clients;

public sealed class ClientUseCase(IClientRepository clientRepository)
{
    public async Task<ClientResponse> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        ValidateName(request.FullName);
        ValidateStatus(request.Status);

        var client = new Client
        {
            OrganizationId = request.OrganizationId,
            FullName = request.FullName,
            Status = request.Status
        };

        var created = await clientRepository.CreateAsync(client, cancellationToken);

        return new ClientResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            FullName = created.FullName,
            Status = created.Status,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<ClientResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var clients = await clientRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);

        return clients.Select(client => new ClientResponse
        {
            Id = client.Id,
            OrganizationId = client.OrganizationId,
            FullName = client.FullName,
            Status = client.Status,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        }).ToList();
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(organizationId, id, cancellationToken);
        if (client is null)
        {
            return null;
        }

        return new ClientResponse
        {
            Id = client.Id,
            OrganizationId = client.OrganizationId,
            FullName = client.FullName,
            Status = client.Status,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }

    public async Task<ClientResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        ValidateName(request.FullName);
        ValidateStatus(request.Status);

        var client = new Client
        {
            Id = id,
            OrganizationId = organizationId,
            FullName = request.FullName,
            Status = request.Status
        };

        var updated = await clientRepository.UpdateAsync(client, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        return new ClientResponse
        {
            Id = updated.Id,
            OrganizationId = updated.OrganizationId,
            FullName = updated.FullName,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return clientRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("FullName is required.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }
    }
}
