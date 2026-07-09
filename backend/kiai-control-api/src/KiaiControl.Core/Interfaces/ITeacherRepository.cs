using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface ITeacherRepository
{
    Task<Teacher> CreateAsync(Teacher teacher, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Teacher?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<Teacher?> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
}
