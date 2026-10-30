using KiaiControl.Contracts.ClassGroups;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.ClassGroups;

public sealed class ClassGroupUseCase(IClassGroupRepository classGroupRepository)
{
    private static readonly string[] AllowedStatuses = ["active", "inactive", "archived"];

    public async Task<ClassGroupResponse?> CreateAsync(
        Guid organizationId,
        CreateClassGroupRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.ModalityId, request.Name, request.Description, request.Capacity, "active");
        ValidateBranchId(request.BranchId);

        var classGroup = new ClassGroup
        {
            OrganizationId = organizationId,
            BranchId = request.BranchId,
            ModalityId = request.ModalityId,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Capacity = request.Capacity,
            Status = "active"
        };

        var created = await classGroupRepository.CreateAsync(classGroup, cancellationToken);
        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<ClassGroupResponse>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var classGroups = await classGroupRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return classGroups.Select(ToResponse).ToList();
    }

    public async Task<ClassGroupResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var classGroup = await classGroupRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return classGroup is null ? null : ToResponse(classGroup);
    }

    public async Task<ClassGroupResponse?> UpdateAsync(
        Guid organizationId,
        Guid id,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.ModalityId, request.Name, request.Description, request.Capacity, request.Status);
        ValidateBranchId(request.BranchId);

        var classGroup = new ClassGroup
        {
            Id = id,
            OrganizationId = organizationId,
            BranchId = request.BranchId,
            ModalityId = request.ModalityId,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Capacity = request.Capacity,
            Status = request.Status.Trim().ToLowerInvariant()
        };

        var updated = await classGroupRepository.UpdateAsync(classGroup, cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    public Task<bool> ArchiveAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return classGroupRepository.ArchiveAsync(organizationId, id, cancellationToken);
    }

    public async Task<ClassScheduleResponse?> CreateScheduleAsync(
        Guid organizationId,
        Guid classGroupId,
        CreateClassScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || classGroupId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ClassGroupId are required.");
        }

        if (request.DayOfWeek is < 0 or > 6 || request.EndsAt <= request.StartsAt)
        {
            throw new ArgumentException("DayOfWeek must be between 0 and 6 and EndsAt must be later than StartsAt.");
        }

        var schedule = new ClassSchedule
        {
            ClassGroupId = classGroupId,
            DayOfWeek = request.DayOfWeek,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        };

        var created = await classGroupRepository.CreateScheduleAsync(organizationId, schedule, cancellationToken);
        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<ClassScheduleResponse>> ListSchedulesAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        var items = await classGroupRepository.ListSchedulesAsync(organizationId, classGroupId, cancellationToken);
        return items.Select(item => new ClassScheduleResponse
        {
            Id = item.Id,
            ClassGroupId = item.ClassGroupId,
            DayOfWeek = item.DayOfWeek,
            StartsAt = TimeOnly.FromTimeSpan(item.StartsAt),
            EndsAt = TimeOnly.FromTimeSpan(item.EndsAt),
            CreatedAt = item.CreatedAt
        }).ToList();
    }

    public Task<bool> DeleteScheduleAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        return classGroupRepository.DeleteScheduleAsync(organizationId, classGroupId, scheduleId, cancellationToken);
    }

    public async Task<EnrollmentResponse?> CreateEnrollmentAsync(
        Guid organizationId,
        Guid classGroupId,
        CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || classGroupId == Guid.Empty || request.ClientId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, ClassGroupId and ClientId are required.");
        }

        var enrollment = new Enrollment
        {
            OrganizationId = organizationId,
            ClassGroupId = classGroupId,
            ClientId = request.ClientId
        };

        var created = await classGroupRepository.CreateEnrollmentAsync(enrollment, cancellationToken);
        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<EnrollmentResponse>> ListEnrollmentsAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        var items = await classGroupRepository.ListEnrollmentsAsync(organizationId, classGroupId, cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public Task<bool> CancelEnrollmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid enrollmentId,
        CancellationToken cancellationToken)
    {
        return classGroupRepository.CancelEnrollmentAsync(organizationId, classGroupId, enrollmentId, cancellationToken);
    }

    public Task<bool> AssignTeacherAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        bool isPrimary,
        DateOnly startsAt,
        DateOnly? endsAt,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || classGroupId == Guid.Empty || teacherId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, ClassGroupId and TeacherId are required.");
        }

        if (endsAt < startsAt)
        {
            throw new ArgumentException("EndsAt must be on or after StartsAt.");
        }

        return classGroupRepository.AssignTeacherAsync(
            organizationId, classGroupId, teacherId, isPrimary, startsAt, endsAt, cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherAssignmentResponse>> ListTeacherAssignmentsAsync(
        Guid organizationId,
        Guid classGroupId,
        CancellationToken cancellationToken)
    {
        var items = await classGroupRepository.ListTeacherAssignmentsAsync(organizationId, classGroupId, cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public Task<bool> RemoveTeacherAssignmentAsync(
        Guid organizationId,
        Guid classGroupId,
        Guid teacherId,
        DateOnly startsAt,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || classGroupId == Guid.Empty || teacherId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, ClassGroupId and TeacherId are required.");
        }

        return classGroupRepository.RemoveTeacherAssignmentAsync(
            organizationId, classGroupId, teacherId, startsAt, cancellationToken);
    }

    private static void Validate(
        Guid organizationId,
        Guid modalityId,
        string name,
        string? description,
        int capacity,
        string status)
    {
        if (organizationId == Guid.Empty || modalityId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ModalityId are required.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 140)
        {
            throw new ArgumentException("Name is required and must not exceed 140 characters.");
        }

        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than zero.");
        }

        if (description?.Length > 5000)
        {
            throw new ArgumentException("Description must not exceed 5000 characters.");
        }

        if (!AllowedStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Status must be active, inactive or archived.");
        }
    }

    private static void ValidateBranchId(Guid? branchId)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId must be a valid identifier when provided.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static ClassGroupResponse ToResponse(ClassGroup classGroup)
    {
        return new ClassGroupResponse
        {
            Id = classGroup.Id,
            OrganizationId = classGroup.OrganizationId,
            BranchId = classGroup.BranchId,
            ModalityId = classGroup.ModalityId,
            Name = classGroup.Name,
            Description = classGroup.Description,
            Capacity = classGroup.Capacity,
            Status = classGroup.Status,
            CreatedAt = classGroup.CreatedAt,
            UpdatedAt = classGroup.UpdatedAt
        };
    }

    private static ClassScheduleResponse ToResponse(ClassSchedule schedule)
    {
        return new ClassScheduleResponse
        {
            Id = schedule.Id,
            ClassGroupId = schedule.ClassGroupId,
            DayOfWeek = schedule.DayOfWeek,
            StartsAt = schedule.StartsAt,
            EndsAt = schedule.EndsAt,
            CreatedAt = schedule.CreatedAt
        };
    }

    private static EnrollmentResponse ToResponse(Enrollment enrollment)
    {
        return new EnrollmentResponse
        {
            Id = enrollment.Id,
            OrganizationId = enrollment.OrganizationId,
            ClassGroupId = enrollment.ClassGroupId,
            ClientId = enrollment.ClientId,
            ClientName = enrollment.ClientName,
            EnrolledAt = enrollment.EnrolledAt,
            EndedAt = enrollment.EndedAt,
            Status = enrollment.Status
        };
    }

    private static TeacherAssignmentResponse ToResponse(ClassGroupTeacherAssignment assignment)
    {
        return new TeacherAssignmentResponse
        {
            Id = assignment.Id,
            ClassGroupId = assignment.ClassGroupId,
            TeacherId = assignment.TeacherId,
            TeacherName = assignment.TeacherName,
            IsPrimary = assignment.IsPrimary,
            StartsAt = DateOnly.FromDateTime(assignment.StartsAt),
            EndsAt = assignment.EndsAt is null ? null : DateOnly.FromDateTime(assignment.EndsAt.Value)
        };
    }
}