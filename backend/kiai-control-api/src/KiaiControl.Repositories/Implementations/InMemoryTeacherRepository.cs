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

    public Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _teachers
            .Where(teacher => teacher.OrganizationId == organizationId)
            .OrderByDescending(teacher => teacher.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Teacher>>(items);
    }

    public Task<Teacher?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = _teachers.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(teacher);
    }

    public Task<Teacher?> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        var index = _teachers.FindIndex(item => item.OrganizationId == teacher.OrganizationId && item.Id == teacher.Id);
        if (index < 0)
        {
            return Task.FromResult<Teacher?>(null);
        }

        var current = _teachers[index];
        current.FullName = teacher.FullName;
        current.Status = teacher.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Teacher?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _teachers.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}
