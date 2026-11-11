using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;
public interface ICatalogRepository
{
    Task<PagedResult<Lesson>> LessonsAsync(Guid organizationId, int page, int pageSize, DateTimeOffset? from, DateTimeOffset? to, string? status, Guid? teacherId, CancellationToken ct, string area = "shared");
    Task<PagedResult<Client>> ClientsAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared");
    Task<PagedResult<Teacher>> TeachersAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared");
    Task<PagedResult<Branch>> BranchesAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken);
    Task<PagedResult<Modality>> ModalitiesAsync(Guid organizationId, int page, int pageSize, string? search, CancellationToken cancellationToken, string area = "shared");
}
