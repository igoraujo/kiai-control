using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface ITeacherRepository
{
    Task<Teacher> CreateAsync(Teacher teacher, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
