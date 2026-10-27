using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Common;

namespace KiaiControl.UseCases.Clients;

public sealed class ClientUseCase(IClientRepository clientRepository, IBranchRepository branchRepository)
{
    public async Task<ClientResponse> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        await PersonDataValidator.ValidateBranchAsync(branchRepository, request.OrganizationId, request.BranchId, cancellationToken);

        var client = new Client
        {
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            FullName = PersonDataValidator.NormalizeName(request.FullName),
            PreferredName = PersonDataValidator.NormalizeOptional(request.PreferredName, "PreferredName", 100),
            Email = PersonDataValidator.NormalizeEmail(request.Email),
            Phone = PersonDataValidator.NormalizeOptional(request.Phone, "Phone", 30),
            BirthDate = request.BirthDate,
            Document = PersonDataValidator.NormalizeDocument(request.Document),
            MedicalNotes = PersonDataValidator.NormalizeOptional(request.MedicalNotes, "MedicalNotes", int.MaxValue),
            EmergencyContactName = PersonDataValidator.NormalizeOptional(request.EmergencyContactName, "EmergencyContactName", 160),
            EmergencyContactPhone = PersonDataValidator.NormalizeOptional(request.EmergencyContactPhone, "EmergencyContactPhone", 30),
            HideFromRanking = request.HideFromRanking,
            RegistrationCode = PersonDataValidator.NormalizeOptional(request.RegistrationCode, "RegistrationCode", 40),
            JoinedAt = request.JoinedAt,
            Status = PersonDataValidator.NormalizeStatus(request.Status)
        };

        return Map(await clientRepository.CreateAsync(client, cancellationToken));
    }

    public async Task<IReadOnlyList<ClientResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var clients = await clientRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return clients.Select(Map).ToList();
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return client is null ? null : Map(client);
    }

    public async Task<ClientResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        await PersonDataValidator.ValidateBranchAsync(branchRepository, organizationId, request.BranchId, cancellationToken);

        var client = new Client
        {
            Id = id,
            OrganizationId = organizationId,
            BranchId = request.BranchId,
            FullName = PersonDataValidator.NormalizeName(request.FullName),
            PreferredName = PersonDataValidator.NormalizeOptional(request.PreferredName, "PreferredName", 100),
            Email = PersonDataValidator.NormalizeEmail(request.Email),
            Phone = PersonDataValidator.NormalizeOptional(request.Phone, "Phone", 30),
            BirthDate = request.BirthDate,
            Document = PersonDataValidator.NormalizeDocument(request.Document),
            MedicalNotes = PersonDataValidator.NormalizeOptional(request.MedicalNotes, "MedicalNotes", int.MaxValue),
            EmergencyContactName = PersonDataValidator.NormalizeOptional(request.EmergencyContactName, "EmergencyContactName", 160),
            EmergencyContactPhone = PersonDataValidator.NormalizeOptional(request.EmergencyContactPhone, "EmergencyContactPhone", 30),
            HideFromRanking = request.HideFromRanking,
            RegistrationCode = PersonDataValidator.NormalizeOptional(request.RegistrationCode, "RegistrationCode", 40),
            JoinedAt = request.JoinedAt,
            Status = PersonDataValidator.NormalizeStatus(request.Status)
        };

        var updated = await clientRepository.UpdateAsync(client, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return clientRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    private static ClientResponse Map(Client client) => new()
    {
        Id = client.Id,
        OrganizationId = client.OrganizationId,
        PersonId = client.PersonId,
        BranchId = client.BranchId,
        FullName = client.FullName,
        PreferredName = client.PreferredName,
        Email = client.Email,
        Phone = client.Phone,
        BirthDate = client.BirthDate,
        Document = client.Document,
        MedicalNotes = client.MedicalNotes,
        EmergencyContactName = client.EmergencyContactName,
        EmergencyContactPhone = client.EmergencyContactPhone,
        HideFromRanking = client.HideFromRanking,
        RegistrationCode = client.RegistrationCode,
        JoinedAt = client.JoinedAt,
        Status = client.Status,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt
    };
}