namespace KiaiControl.Contracts.ClassGroups;

public sealed class CreateClassGroupRequest
{
    public Guid? BranchId { get; set; }

    public Guid ModalityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Capacity { get; set; }
}

public sealed class UpdateClassGroupRequest
{
    public Guid? BranchId { get; set; }

    public Guid ModalityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Capacity { get; set; }

    public string Status { get; set; } = "active";
}

public sealed class ClassGroupResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid ModalityId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int Capacity { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateClassScheduleRequest
{
    public int DayOfWeek { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }
}

public sealed class ClassScheduleResponse
{
    public Guid Id { get; init; }

    public Guid ClassGroupId { get; init; }

    public int DayOfWeek { get; init; }

    public TimeOnly StartsAt { get; init; }

    public TimeOnly EndsAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateEnrollmentRequest
{
    public Guid ClientId { get; set; }
}

public sealed class AssignClassGroupTeacherRequest
{
    public bool IsPrimary { get; set; }

    public DateOnly StartsAt { get; set; }

    public DateOnly? EndsAt { get; set; }
}

public sealed class EnrollmentResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid ClassGroupId { get; init; }

    public Guid ClientId { get; init; }

    public string ClientName { get; init; } = string.Empty;

    public DateTimeOffset EnrolledAt { get; init; }

    public DateTimeOffset? EndedAt { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class TeacherAssignmentResponse
{
    public Guid Id { get; init; }

    public Guid ClassGroupId { get; init; }

    public Guid TeacherId { get; init; }

    public string TeacherName { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public DateOnly StartsAt { get; init; }

    public DateOnly? EndsAt { get; init; }
}