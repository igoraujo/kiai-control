using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;
public interface IAdministrationRepository
{
    Task<PagedResult<DirectoryPerson>> ListPeopleAsync(Guid organizationId, int page, int size, string? search, CancellationToken ct);
    Task<Guid> SavePersonAsync(Guid organizationId, Guid? personId, string name, string? email, string? phone, CancellationToken ct);
    Task<bool> AddProfileAsync(Guid organizationId, Guid personId, string profile, Guid actorUserId, CancellationToken ct);
    Task<bool> ManageAccountAsync(Guid organizationId, Guid personId, Guid actorUserId, bool actorIsOwner, bool? active, string? passwordHash, CancellationToken ct, string? email = null);
    Task<OrganizationSettings> GetSettingsAsync(Guid organizationId, CancellationToken ct);
    Task SaveSettingsAsync(Guid organizationId, OrganizationSettings settings, CancellationToken ct);
}
