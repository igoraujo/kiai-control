using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IStudentRepository
{
    Task<Student> CreateAsync(Student student, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Student>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
