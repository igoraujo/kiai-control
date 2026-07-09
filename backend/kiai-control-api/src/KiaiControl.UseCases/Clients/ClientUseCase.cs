using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Clients;

public sealed class ClientUseCase(IClientRepository clientRepository)
{
    public async Task<ClientResponse> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
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
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IReadOnlyList<ClientResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var clients = await clientRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        return clients.Select(client => new ClientResponse
        {
            Id = client.Id,
            OrganizationId = client.OrganizationId,
            FullName = client.FullName,
            Status = client.Status,
            CreatedAt = client.CreatedAt
        }).ToList();
    }
}
