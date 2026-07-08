using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryTeacherRepository : ITeacherRepository
{
    private readonly List<Teacher> _teachers = [];

    public Task<Teacher> CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.Id = Guid.NewGuid();
        teacher.CreatedAt = DateTimeOffset.UtcNow;
        _teachers.Add(teacher);
        return Task.FromResult(teacher);
    }

    public Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var items = _teachers.Where(teacher => teacher.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<Teacher>>(items);
    }

    public Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = _teachers.FirstOrDefault(item => item.Id == id);
        return Task.FromResult(teacher);
    }
}
