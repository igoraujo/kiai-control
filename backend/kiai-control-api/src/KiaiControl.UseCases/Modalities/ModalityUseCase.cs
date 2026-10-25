using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Modalities;

public sealed class ModalityUseCase(IModalityRepository modalityRepository)
{
    private static readonly string[] AllowedTypes = ["martial_art", "fitness", "hybrid"];

    public async Task<ModalityResponse> CreateAsync(
        Guid organizationId,
        CreateModalityRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.Name, request.Type, request.Description);

        var modality = new Modality
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Type = NormalizeType(request.Type),
            Description = NormalizeDescription(request.Description),
            IsActive = true
        };

        return ToResponse(await modalityRepository.CreateAsync(modality, cancellationToken));
    }

    public async Task<IReadOnlyList<ModalityResponse>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var modalities = await modalityRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return modalities.Select(ToResponse).ToList();
    }

    public async Task<ModalityResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var modality = await modalityRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return modality is null ? null : ToResponse(modality);
    }

    public async Task<ModalityResponse?> UpdateAsync(
        Guid organizationId,
        Guid id,
        UpdateModalityRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.Name, request.Type, request.Description);

        var modality = new Modality
        {
            Id = id,
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Type = NormalizeType(request.Type),
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive
        };

        var updated = await modalityRepository.UpdateAsync(modality, cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return modalityRepository.DeactivateAsync(organizationId, id, cancellationToken);
    }

    public Task<bool> LinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken)
    {
        ValidateLinkIds(organizationId, clientId, modalityId);
        return modalityRepository.LinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
    }

    public Task<bool> UnlinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken)
    {
        ValidateLinkIds(organizationId, clientId, modalityId);
        return modalityRepository.UnlinkClientAsync(organizationId, clientId, modalityId, cancellationToken);
    }

    private static void ValidateLinkIds(Guid organizationId, Guid clientId, Guid modalityId)
    {
        if (organizationId == Guid.Empty || clientId == Guid.Empty || modalityId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, ClientId and ModalityId are required.");
        }
    }

    private static void Validate(Guid organizationId, string name, string type, string? description)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            throw new ArgumentException("Name is required and must not exceed 120 characters.");
        }

        if (!AllowedTypes.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Type must be martial_art, fitness or hybrid.");
        }

        if (description?.Length > 5000)
        {
            throw new ArgumentException("Description must not exceed 5000 characters.");
        }
    }

    private static string NormalizeType(string type)
    {
        return AllowedTypes.First(allowedType => string.Equals(allowedType, type.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static ModalityResponse ToResponse(Modality modality)
    {
        return new ModalityResponse
        {
            Id = modality.Id,
            OrganizationId = modality.OrganizationId,
            Name = modality.Name,
            Type = modality.Type,
            Description = modality.Description,
            IsActive = modality.IsActive,
            CreatedAt = modality.CreatedAt,
            UpdatedAt = modality.UpdatedAt
        };
    }
}