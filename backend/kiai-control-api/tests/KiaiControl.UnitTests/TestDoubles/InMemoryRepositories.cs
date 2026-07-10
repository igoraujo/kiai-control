using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Id = Guid.NewGuid();
        client.CreatedAt = DateTimeOffset.UtcNow;
        _clients.Add(client);
        return Task.FromResult(client);
    }

    public Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _clients
            .Where(client => client.OrganizationId == organizationId)
            .OrderByDescending(client => client.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Client>>(items);
    }

    public Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var client = _clients.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(client);
    }

    public Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        var index = _clients.FindIndex(item => item.OrganizationId == client.OrganizationId && item.Id == client.Id);
        if (index < 0)
        {
            return Task.FromResult<Client?>(null);
        }

        var current = _clients[index];
        current.FullName = client.FullName;
        current.Status = client.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Client?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _clients.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}

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

public sealed class InMemoryAttendanceRepository : IAttendanceRepository
{
    private readonly List<AttendanceRecord> _attendance = [];

    public Task<AttendanceRecord> CreateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        attendance.Id = Guid.NewGuid();
        attendance.CreatedAt = DateTimeOffset.UtcNow;
        _attendance.Add(attendance);
        return Task.FromResult(attendance);
    }

    public Task<IReadOnlyList<AttendanceRecord>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _attendance
            .Where(record => record.OrganizationId == organizationId)
            .OrderByDescending(record => record.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<AttendanceRecord>>(items);
    }

    public Task<AttendanceRecord?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var attendance = _attendance.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(attendance);
    }

    public Task<AttendanceRecord?> UpdateAsync(AttendanceRecord attendance, CancellationToken cancellationToken = default)
    {
        var index = _attendance.FindIndex(item => item.OrganizationId == attendance.OrganizationId && item.Id == attendance.Id);
        if (index < 0)
        {
            return Task.FromResult<AttendanceRecord?>(null);
        }

        var current = _attendance[index];
        current.LessonId = attendance.LessonId;
        current.ClientId = attendance.ClientId;
        current.AttendanceDate = attendance.AttendanceDate;
        current.Status = attendance.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<AttendanceRecord?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _attendance.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}

public sealed class InMemoryBillingRepository : IBillingRepository
{
    private readonly List<Billing> _billing = [];

    public Task<Billing> CreateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        billing.Id = Guid.NewGuid();
        billing.CreatedAt = DateTimeOffset.UtcNow;
        _billing.Add(billing);
        return Task.FromResult(billing);
    }

    public Task<IReadOnlyList<Billing>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _billing
            .Where(record => record.OrganizationId == organizationId)
            .OrderByDescending(record => record.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Billing>>(items);
    }

    public Task<Billing?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var billing = _billing.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(billing);
    }

    public Task<Billing?> UpdateAsync(Billing billing, CancellationToken cancellationToken = default)
    {
        var index = _billing.FindIndex(item => item.OrganizationId == billing.OrganizationId && item.Id == billing.Id);
        if (index < 0)
        {
            return Task.FromResult<Billing?>(null);
        }

        var current = _billing[index];
        current.ClientPlanSubscriptionId = billing.ClientPlanSubscriptionId;
        current.ClientId = billing.ClientId;
        current.DueDate = billing.DueDate;
        current.Amount = billing.Amount;
        current.Status = billing.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Billing?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _billing.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0;
        return Task.FromResult(removed);
    }
}