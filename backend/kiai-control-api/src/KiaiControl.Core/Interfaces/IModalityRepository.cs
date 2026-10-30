using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IModalityRepository
{
    Task<Modality> CreateAsync(Modality modality, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Modality>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Modality?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Modality?> UpdateAsync(Modality modality, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Modality>> ListClientModalitiesAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default);

    Task<bool> LinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default);

    Task<bool> UnlinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default);
}