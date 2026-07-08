using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = [];

    public Task<Student> CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        student.Id = Guid.NewGuid();
        student.CreatedAt = DateTimeOffset.UtcNow;
        _students.Add(student);
        return Task.FromResult(student);
    }

    public Task<IReadOnlyList<Student>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = _students.Where(student => student.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<Student>>(items);
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = _students.FirstOrDefault(item => item.Id == id);
        return Task.FromResult(student);
    }
}
