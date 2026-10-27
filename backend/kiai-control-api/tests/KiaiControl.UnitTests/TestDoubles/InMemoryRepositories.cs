using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Repositories.Implementations;

public sealed class InMemoryBranchRepository : IBranchRepository
{
    private readonly List<Branch> _branches = [];

    public void Add(Branch branch) => _branches.Add(branch);

    public Task<Branch?> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        branch.Id = Guid.NewGuid();
        branch.CreatedAt = DateTimeOffset.UtcNow;
        _branches.Add(branch);
        return Task.FromResult<Branch?>(branch);
    }

    public Task<IReadOnlyList<Branch>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var branches = _branches.Where(item => item.OrganizationId == organizationId).ToList();
        return Task.FromResult<IReadOnlyList<Branch>>(branches);
    }

    public Task<Branch?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_branches.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id));
    }

    public Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        var existing = _branches.FirstOrDefault(item => item.OrganizationId == branch.OrganizationId && item.Id == branch.Id);
        return Task.FromResult(existing);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_branches.RemoveAll(item => item.OrganizationId == organizationId && item.Id == id) > 0);
    }
}

public sealed class InMemoryClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        client.Id = Guid.NewGuid();
        client.PersonId = Guid.NewGuid();
        client.CreatedAt = DateTimeOffset.UtcNow;
        _clients.Add(client);
        return Task.FromResult(client);
    }

    public Task<IReadOnlyList<Client>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _clients
            .Where(client => client.OrganizationId == organizationId && client.DeletedAt is null)
            .OrderByDescending(client => client.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Client>>(items);
    }

    public Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var client = _clients.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.DeletedAt is null);
        return Task.FromResult(client);
    }

    public Task<Client?> UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        var index = _clients.FindIndex(item => item.OrganizationId == client.OrganizationId && item.Id == client.Id && item.DeletedAt is null);
        if (index < 0)
        {
            return Task.FromResult<Client?>(null);
        }

        var current = _clients[index];
        current.BranchId = client.BranchId;
        current.FullName = client.FullName;
        current.PreferredName = client.PreferredName;
        current.Email = client.Email;
        current.Phone = client.Phone;
        current.BirthDate = client.BirthDate;
        current.Document = client.Document;
        current.MedicalNotes = client.MedicalNotes;
        current.EmergencyContactName = client.EmergencyContactName;
        current.EmergencyContactPhone = client.EmergencyContactPhone;
        current.HideFromRanking = client.HideFromRanking;
        current.RegistrationCode = client.RegistrationCode;
        current.JoinedAt = client.JoinedAt;
        current.Status = client.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Client?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var client = _clients.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.DeletedAt is null);
        if (client is null)
        {
            return Task.FromResult(false);
        }

        client.DeletedAt = DateTimeOffset.UtcNow;
        client.Status = "inactive";
        return Task.FromResult(true);
    }
}

public sealed class InMemoryTeacherRepository : ITeacherRepository
{
    private readonly List<Teacher> _teachers = [];

    public Task<Teacher> CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.Id = Guid.NewGuid();
        teacher.PersonId = Guid.NewGuid();
        teacher.CreatedAt = DateTimeOffset.UtcNow;
        _teachers.Add(teacher);
        return Task.FromResult(teacher);
    }

    public Task<IReadOnlyList<Teacher>> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var items = _teachers
            .Where(teacher => teacher.OrganizationId == organizationId && teacher.DeletedAt is null)
            .OrderByDescending(teacher => teacher.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Teacher>>(items);
    }

    public Task<Teacher?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = _teachers.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.DeletedAt is null);
        return Task.FromResult(teacher);
    }

    public Task<Teacher?> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        var index = _teachers.FindIndex(item => item.OrganizationId == teacher.OrganizationId && item.Id == teacher.Id && item.DeletedAt is null);
        if (index < 0)
        {
            return Task.FromResult<Teacher?>(null);
        }

        var current = _teachers[index];
        current.BranchId = teacher.BranchId;
        current.FullName = teacher.FullName;
        current.PreferredName = teacher.PreferredName;
        current.Email = teacher.Email;
        current.Phone = teacher.Phone;
        current.BirthDate = teacher.BirthDate;
        current.Document = teacher.Document;
        current.MedicalNotes = teacher.MedicalNotes;
        current.EmergencyContactName = teacher.EmergencyContactName;
        current.EmergencyContactPhone = teacher.EmergencyContactPhone;
        current.HideFromRanking = teacher.HideFromRanking;
        current.HiredAt = teacher.HiredAt;
        current.ActiveFrom = teacher.ActiveFrom;
        current.Status = teacher.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Teacher?>(current);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = _teachers.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.DeletedAt is null);
        if (teacher is null)
        {
            return Task.FromResult(false);
        }

        teacher.DeletedAt = DateTimeOffset.UtcNow;
        teacher.Status = "inactive";
        return Task.FromResult(true);
    }
}

public sealed class InMemoryModalityRepository : IModalityRepository
{
    private readonly List<Modality> _modalities = [];
    private readonly HashSet<(Guid OrganizationId, Guid ClientId, Guid ModalityId)> _clientModalities = [];

    public Task<Modality> CreateAsync(Modality modality, CancellationToken cancellationToken = default)
    {
        modality.Id = Guid.NewGuid();
        modality.CreatedAt = DateTimeOffset.UtcNow;
        _modalities.Add(modality);
        return Task.FromResult(modality);
    }

    public Task<IReadOnlyList<Modality>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var modalities = _modalities
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Modality>>(modalities);
    }

    public Task<Modality?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var modality = _modalities.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(modality);
    }

    public Task<Modality?> UpdateAsync(Modality modality, CancellationToken cancellationToken = default)
    {
        var current = _modalities.FirstOrDefault(item => item.OrganizationId == modality.OrganizationId && item.Id == modality.Id);
        if (current is null)
        {
            return Task.FromResult<Modality?>(null);
        }

        current.Name = modality.Name;
        current.Type = modality.Type;
        current.Description = modality.Description;
        current.IsActive = modality.IsActive;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Modality?>(current);
    }

    public Task<bool> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var modality = _modalities.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.IsActive);
        if (modality is null)
        {
            return Task.FromResult(false);
        }

        modality.IsActive = false;
        modality.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> LinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        var modality = _modalities.FirstOrDefault(item => item.OrganizationId == organizationId
            && item.Id == modalityId && item.IsActive);
        return Task.FromResult(modality is not null && _clientModalities.Add((organizationId, clientId, modalityId)));
    }

    public Task<bool> UnlinkClientAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_clientModalities.Remove((organizationId, clientId, modalityId)));
    }
}

public sealed class InMemoryClassGroupRepository : IClassGroupRepository
{
    private readonly List<ClassGroup> _classGroups = [];
    private readonly List<ClassSchedule> _schedules = [];
    private readonly List<Enrollment> _enrollments = [];
    private readonly HashSet<(Guid OrganizationId, Guid ClassGroupId, Guid TeacherId, DateOnly StartsAt)> _teacherAssignments = [];

    public Task<ClassGroup?> CreateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default)
    {
        classGroup.Id = Guid.NewGuid();
        classGroup.CreatedAt = DateTimeOffset.UtcNow;
        _classGroups.Add(classGroup);
        return Task.FromResult<ClassGroup?>(classGroup);
    }

    public Task<IReadOnlyList<ClassGroup>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var classGroups = _classGroups
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<ClassGroup>>(classGroups);
    }

    public Task<ClassGroup?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var classGroup = _classGroups.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(classGroup);
    }

    public Task<ClassGroup?> UpdateAsync(ClassGroup classGroup, CancellationToken cancellationToken = default)
    {
        var current = _classGroups.FirstOrDefault(item => item.OrganizationId == classGroup.OrganizationId && item.Id == classGroup.Id);
        if (current is null)
        {
            return Task.FromResult<ClassGroup?>(null);
        }

        current.BranchId = classGroup.BranchId;
        current.ModalityId = classGroup.ModalityId;
        current.Name = classGroup.Name;
        current.Description = classGroup.Description;
        current.Capacity = classGroup.Capacity;
        current.Status = classGroup.Status;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<ClassGroup?>(current);
    }

    public Task<bool> ArchiveAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var classGroup = _classGroups.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id && item.Status != "archived");
        if (classGroup is null)
        {
            return Task.FromResult(false);
        }

        classGroup.Status = "archived";
        classGroup.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }

    public Task<ClassSchedule?> CreateScheduleAsync(
        Guid organizationId,
        ClassSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        if (!_classGroups.Any(item => item.OrganizationId == organizationId && item.Id == schedule.ClassGroupId && item.Status == "active")
            || _schedules.Any(item => item.ClassGroupId == schedule.ClassGroupId
                && item.DayOfWeek == schedule.DayOfWeek
                && item.StartsAt == schedule.StartsAt))
        {
            return Task.FromResult<ClassSchedule?>(null);
        }

        schedule.Id = Guid.NewGuid();
        schedule.CreatedAt = DateTimeOffset.UtcNow;
        _schedules.Add(schedule);
        return Task.FromResult<ClassSchedule?>(schedule);
    }

    public Task<bool> DeleteScheduleAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        if (!_classGroups.Any(item => item.OrganizationId == organizationId && item.Id == classGroupId))
        {
            return Task.FromResult(false);
        }

        var removed = _schedules.RemoveAll(item => item.ClassGroupId == classGroupId && item.Id == scheduleId) > 0;
        return Task.FromResult(removed);
    }

    public Task<Enrollment?> CreateEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        var classGroup = _classGroups.FirstOrDefault(item => item.OrganizationId == enrollment.OrganizationId
            && item.Id == enrollment.ClassGroupId && item.Status == "active");
        if (classGroup is null)
        {
            return Task.FromResult<Enrollment?>(null);
        }

        var existing = _enrollments.FirstOrDefault(item => item.OrganizationId == enrollment.OrganizationId
            && item.ClassGroupId == enrollment.ClassGroupId && item.ClientId == enrollment.ClientId);
        var otherActiveEnrollments = _enrollments.Count(item => item.OrganizationId == enrollment.OrganizationId
            && item.ClassGroupId == enrollment.ClassGroupId && item.ClientId != enrollment.ClientId && item.Status == "active");
        if (otherActiveEnrollments >= classGroup.Capacity)
        {
            return Task.FromResult<Enrollment?>(null);
        }

        if (existing is not null)
        {
            existing.Status = "active";
            existing.EndedAt = null;
            existing.EnrolledAt = DateTimeOffset.UtcNow;
            return Task.FromResult<Enrollment?>(existing);
        }

        enrollment.Id = Guid.NewGuid();
        enrollment.EnrolledAt = DateTimeOffset.UtcNow;
        _enrollments.Add(enrollment);
        return Task.FromResult<Enrollment?>(enrollment);
    }

    public Task<bool> CancelEnrollmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = _enrollments.FirstOrDefault(item => item.OrganizationId == organizationId
            && item.ClassGroupId == classGroupId && item.Id == enrollmentId && item.Status == "active");
        if (enrollment is null)
        {
            return Task.FromResult(false);
        }

        enrollment.Status = "canceled";
        enrollment.EndedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> AssignTeacherAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        bool isPrimary,
        DateOnly startsAt,
        DateOnly? endsAt,
        CancellationToken cancellationToken = default)
    {
        var classGroupExists = _classGroups.Any(item => item.OrganizationId == organizationId
            && item.Id == classGroupId && item.Status == "active");
        return Task.FromResult(classGroupExists && _teacherAssignments.Add((organizationId, classGroupId, teacherId, startsAt)));
    }

    public Task<bool> RemoveTeacherAssignmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        DateOnly startsAt,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_teacherAssignments.Remove((organizationId, classGroupId, teacherId, startsAt)));
    }
}

public sealed class InMemoryLessonRepository : ILessonRepository
{
    private readonly List<Lesson> _lessons = [];

    public Task<Lesson?> CreateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        lesson.Id = Guid.NewGuid();
        lesson.CreatedAt = DateTimeOffset.UtcNow;
        _lessons.Add(lesson);
        return Task.FromResult<Lesson?>(lesson);
    }

    public Task<IReadOnlyList<Lesson>> ListByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var lessons = _lessons
            .Where(item => item.OrganizationId == organizationId)
            .OrderByDescending(item => item.StartsAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<Lesson>>(lessons);
    }

    public Task<Lesson?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = _lessons.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id);
        return Task.FromResult(lesson);
    }

    public Task<Lesson?> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        var current = _lessons.FirstOrDefault(item => item.OrganizationId == lesson.OrganizationId && item.Id == lesson.Id
            && item.Status is "scheduled" or "rescheduled");
        if (current is null)
        {
            return Task.FromResult<Lesson?>(null);
        }

        current.StartsAt = lesson.StartsAt;
        current.EndsAt = lesson.EndsAt;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Lesson?>(current);
    }

    public Task<Lesson?> CancelAsync(
        Guid organizationId,
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var lesson = _lessons.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id
            && item.Status is "scheduled" or "rescheduled");
        if (lesson is null)
        {
            return Task.FromResult<Lesson?>(null);
        }

        lesson.Status = "canceled";
        lesson.CancellationReason = reason;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Lesson?>(lesson);
    }

    public Task<Lesson?> RescheduleAsync(
        Guid organizationId,
        Guid id,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken = default)
    {
        var lesson = _lessons.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == id
            && item.Status is "scheduled" or "rescheduled");
        if (lesson is null)
        {
            return Task.FromResult<Lesson?>(null);
        }

        lesson.StartsAt = startsAt;
        lesson.EndsAt = endsAt;
        lesson.Status = "rescheduled";
        lesson.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Lesson?>(lesson);
    }

    public Task<IReadOnlyList<Lesson>?> GenerateFromSchedulesAsync(
        Guid organizationId,
        Guid classGroupId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var classGroupExists = _lessons.Any(item => item.OrganizationId == organizationId && item.ClassGroupId == classGroupId);
        return Task.FromResult<IReadOnlyList<Lesson>?>(classGroupExists ? [] : null);
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

    public Task<IReadOnlyList<AttendanceRecord>> ListByLessonAsync(
        Guid organizationId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AttendanceRecord> items = _attendance
            .Where(item => item.OrganizationId == organizationId && item.LessonId == lessonId)
            .OrderBy(item => item.CreatedAt)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<AttendanceRecord>?> SaveLessonCallAsync(
        Guid organizationId,
        Guid lessonId,
        IReadOnlyList<AttendanceRecord> entries,
        CancellationToken cancellationToken = default)
    {
        var savedEntries = new List<AttendanceRecord>(entries.Count);
        foreach (var entry in entries)
        {
            var existing = _attendance.FirstOrDefault(item => item.OrganizationId == organizationId
                && item.LessonId == lessonId && item.ClientId == entry.ClientId);
            if (existing is null)
            {
                entry.Id = Guid.NewGuid();
                entry.OrganizationId = organizationId;
                entry.LessonId = lessonId;
                entry.CreatedAt = DateTimeOffset.UtcNow;
                _attendance.Add(entry);
                savedEntries.Add(entry);
                continue;
            }

            existing.AttendanceDate = entry.AttendanceDate;
            existing.Status = entry.Status;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            savedEntries.Add(existing);
        }

        return Task.FromResult<IReadOnlyList<AttendanceRecord>?>(savedEntries);
    }
}

public sealed class InMemoryBillingRepository : IBillingRepository
{
    private readonly List<Billing> _billing = [];
    private readonly List<Payment> _payments = [];

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

    public Task<Payment?> RegisterPaymentAsync(
        Guid organizationId,
        Guid invoiceId,
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        var invoice = _billing.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == invoiceId);
        if (invoice is null || invoice.Status is "canceled" or "exempt" || payment.Amount > invoice.Amount - invoice.PaidAmount)
        {
            return Task.FromResult<Payment?>(null);
        }

        payment.Id = Guid.NewGuid();
        payment.OrganizationId = organizationId;
        payment.InvoiceId = invoiceId;
        payment.CreatedAt = DateTimeOffset.UtcNow;
        _payments.Add(payment);
        invoice.PaidAmount += payment.Amount;
        invoice.Status = invoice.PaidAmount >= invoice.Amount ? "paid" : "partially_paid";
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Payment?>(payment);
    }

    public Task<IReadOnlyList<Payment>> ListPaymentsByOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        IReadOnlyList<Payment> items = _payments
            .Where(item => item.OrganizationId == organizationId)
            .OrderByDescending(item => item.PaidAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<Payment>> ListPaymentsByInvoiceAsync(
        Guid organizationId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Payment> items = _payments
            .Where(item => item.OrganizationId == organizationId && item.InvoiceId == invoiceId)
            .OrderByDescending(item => item.PaidAt)
            .ToList();
        return Task.FromResult(items);
    }
}

public sealed class InMemoryBillingCatalogRepository : IBillingCatalogRepository
{
    private readonly List<BillingPlan> _plans = [];
    private readonly List<ClientPlanSubscription> _subscriptions = [];
    private readonly List<Billing> _invoices = [];

    public Task<BillingPlan> CreatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default)
    {
        plan.Id = Guid.NewGuid();
        plan.CreatedAt = DateTimeOffset.UtcNow;
        _plans.Add(plan);
        return Task.FromResult(plan);
    }

    public Task<IReadOnlyList<BillingPlan>> ListPlansAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        IReadOnlyList<BillingPlan> plans = _plans
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();
        return Task.FromResult(plans);
    }

    public Task<BillingPlan?> UpdatePlanAsync(BillingPlan plan, CancellationToken cancellationToken = default)
    {
        var current = _plans.FirstOrDefault(item => item.OrganizationId == plan.OrganizationId && item.Id == plan.Id);
        if (current is null)
        {
            return Task.FromResult<BillingPlan?>(null);
        }

        current.Name = plan.Name;
        current.Description = plan.Description;
        current.MonthlyAmount = plan.MonthlyAmount;
        current.DueDay = plan.DueDay;
        current.IsActive = plan.IsActive;
        current.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<BillingPlan?>(current);
    }

    public Task<bool> DeactivatePlanAsync(Guid organizationId, Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = _plans.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == planId && item.IsActive);
        if (plan is null)
        {
            return Task.FromResult(false);
        }

        plan.IsActive = false;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }

    public Task<ClientPlanSubscription?> CreateSubscriptionAsync(ClientPlanSubscription subscription, CancellationToken cancellationToken = default)
    {
        if (!_plans.Any(item => item.OrganizationId == subscription.OrganizationId && item.Id == subscription.PlanId && item.IsActive)
            || _subscriptions.Any(item => item.OrganizationId == subscription.OrganizationId && item.ClientId == subscription.ClientId && item.IsActive))
        {
            return Task.FromResult<ClientPlanSubscription?>(null);
        }

        subscription.Id = Guid.NewGuid();
        subscription.CreatedAt = DateTimeOffset.UtcNow;
        _subscriptions.Add(subscription);
        return Task.FromResult<ClientPlanSubscription?>(subscription);
    }

    public Task<ClientPlanSubscription?> CancelSubscriptionAsync(Guid organizationId, Guid subscriptionId, DateOnly endsAt, CancellationToken cancellationToken = default)
    {
        var subscription = _subscriptions.FirstOrDefault(item => item.OrganizationId == organizationId && item.Id == subscriptionId
            && item.IsActive && endsAt >= item.StartsAt);
        if (subscription is null)
        {
            return Task.FromResult<ClientPlanSubscription?>(null);
        }

        subscription.IsActive = false;
        subscription.EndsAt = endsAt;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<ClientPlanSubscription?>(subscription);
    }

    public Task<IReadOnlyList<Billing>?> GenerateInvoicesAsync(Guid organizationId, DateOnly referenceMonth, CancellationToken cancellationToken = default)
    {
        var monthSubscriptions = _subscriptions.Where(item => item.OrganizationId == organizationId && item.IsActive
            && item.StartsAt < referenceMonth.AddMonths(1) && (item.EndsAt is null || item.EndsAt >= referenceMonth));
        var invoices = new List<Billing>();
        foreach (var subscription in monthSubscriptions)
        {
            if (_invoices.Any(item => item.ClientPlanSubscriptionId == subscription.Id && item.DueDate.Month == referenceMonth.Month && item.DueDate.Year == referenceMonth.Year))
            {
                continue;
            }

            var plan = _plans.First(item => item.OrganizationId == organizationId && item.Id == subscription.PlanId);
            var invoice = new Billing
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ClientPlanSubscriptionId = subscription.Id,
                ClientId = subscription.ClientId,
                DueDate = referenceMonth.AddDays(plan.DueDay - 1),
                Amount = plan.MonthlyAmount,
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _invoices.Add(invoice);
            invoices.Add(invoice);
        }

        return Task.FromResult<IReadOnlyList<Billing>?>(invoices);
    }
}