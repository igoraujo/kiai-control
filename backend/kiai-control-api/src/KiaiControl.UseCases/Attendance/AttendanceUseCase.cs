using KiaiControl.Contracts.Attendance;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Attendance;

public sealed class AttendanceUseCase(IAttendanceRepository attendanceRepository)
{
    private static readonly string[] AllowedStatuses = ["present", "absent", "justified_absence", "late"];

    public async Task<AttendanceRecordResponse?> CreateAsync(
        CreateAttendanceRequest request,
        Guid validatorPersonId,
        CancellationToken cancellationToken)
    {
        Validate(request.OrganizationId, request.ClientId, request.LessonId, request.Status);
        ValidateValidator(validatorPersonId);
        var saved = await attendanceRepository.SaveLessonCallAsync(
            request.OrganizationId,
            request.LessonId,
            validatorPersonId,
            [new AttendanceRecord
            {
                ClientId = request.ClientId,
                AttendanceDate = request.AttendanceDate,
                Status = request.Status.Trim().ToLowerInvariant()
            }],
            cancellationToken);
        return saved?.FirstOrDefault() is { } record ? ToResponse(record) : null;
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var attendance = await attendanceRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);

        return attendance.Select(record => new AttendanceRecordResponse
        {
            Id = record.Id,
            OrganizationId = record.OrganizationId,
            LessonId = record.LessonId,
            ClientId = record.ClientId,
            ClientName = record.ClientName,
            AttendanceDate = record.AttendanceDate,
            Status = record.Status,
            ValidatedAt = record.ValidatedAt,
            ValidatedByPersonId = record.ValidatedByPersonId,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        }).ToList();
    }

    public async Task<AttendanceRecordResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var attendance = await attendanceRepository.GetByIdAsync(organizationId, id, cancellationToken);
        if (attendance is null)
        {
            return null;
        }

        return new AttendanceRecordResponse
        {
            Id = attendance.Id,
            OrganizationId = attendance.OrganizationId,
            LessonId = attendance.LessonId,
            ClientId = attendance.ClientId,
            ClientName = attendance.ClientName,
            AttendanceDate = attendance.AttendanceDate,
            Status = attendance.Status,
            ValidatedAt = attendance.ValidatedAt,
            ValidatedByPersonId = attendance.ValidatedByPersonId,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.UpdatedAt
        };
    }

    public async Task<AttendanceRecordResponse?> UpdateAsync(
        Guid organizationId,
        Guid id,
        UpdateAttendanceRequest request,
        Guid validatorPersonId,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, request.ClientId, request.LessonId, request.Status);
        ValidateValidator(validatorPersonId);
        var validatedAt = DateTimeOffset.UtcNow;

        var attendance = new AttendanceRecord
        {
            Id = id,
            OrganizationId = organizationId,
            LessonId = request.LessonId,
            ClientId = request.ClientId,
            AttendanceDate = request.AttendanceDate,
            Status = request.Status.Trim().ToLowerInvariant(),
            ValidatedAt = validatedAt,
            ValidatedByPersonId = validatorPersonId
        };

        var updated = await attendanceRepository.UpdateAsync(attendance, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        return new AttendanceRecordResponse
        {
            Id = updated.Id,
            OrganizationId = updated.OrganizationId,
            LessonId = updated.LessonId,
            ClientId = updated.ClientId,
            ClientName = updated.ClientName,
            AttendanceDate = updated.AttendanceDate,
            Status = updated.Status,
            ValidatedAt = updated.ValidatedAt,
            ValidatedByPersonId = updated.ValidatedByPersonId,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return attendanceRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListByLessonAsync(
        Guid organizationId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || lessonId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and LessonId are required.");
        }

        var records = await attendanceRepository.ListByLessonAsync(organizationId, lessonId, cancellationToken);
        return records.Select(ToResponse).ToList();
    }

    public async Task<AttendanceRecordResponse?> CheckInAsync(
        Guid organizationId,
        Guid personId,
        Guid lessonId,
        DateTimeOffset checkedInAt,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || personId == Guid.Empty || lessonId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId, PersonId and LessonId are required.");
        }

        var record = await attendanceRepository.CreateCheckInAsync(organizationId, personId, lessonId, checkedInAt, cancellationToken);
        return record is null ? null : ToResponse(record);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListPendingAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        var records = await attendanceRepository.ListPendingByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return records.Select(ToResponse).ToList();
    }

    public async Task<AttendanceRecordResponse?> ValidateCheckInAsync(
        Guid organizationId,
        Guid attendanceId,
        Guid validatorPersonId,
        string status,
        DateTimeOffset validatedAt,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || attendanceId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and AttendanceId are required.");
        }

        ValidateValidator(validatorPersonId);
        ValidateStatus(status);
        var finalStatus = status.Trim().ToLowerInvariant();
        if (finalStatus == "justified_absence")
        {
            throw new ArgumentException("A check-in must be confirmed as present or late, or rejected as absent.");
        }

        var record = await attendanceRepository.ValidateCheckInAsync(
            organizationId, attendanceId, validatorPersonId, finalStatus, validatedAt, cancellationToken);
        return record is null ? null : ToResponse(record);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>?> SaveLessonCallAsync(
        Guid organizationId,
        Guid lessonId,
        Guid validatorPersonId,
        SaveAttendanceCallRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || lessonId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and LessonId are required.");
        }

        ValidateValidator(validatorPersonId);

        if (request.Entries.Count == 0)
        {
            throw new ArgumentException("At least one attendance entry is required.");
        }

        if (request.Entries.Any(entry => entry.ClientId == Guid.Empty))
        {
            throw new ArgumentException("Every attendance entry must include a ClientId.");
        }

        if (request.Entries.Select(entry => entry.ClientId).Distinct().Count() != request.Entries.Count)
        {
            throw new ArgumentException("A client can only appear once in an attendance call.");
        }

        foreach (var entry in request.Entries)
        {
            ValidateStatus(entry.Status);
        }

        var records = request.Entries.Select(entry => new AttendanceRecord
        {
            ClientId = entry.ClientId,
            AttendanceDate = entry.AttendanceDate,
            Status = entry.Status.Trim().ToLowerInvariant(),
            ValidatedAt = DateTimeOffset.UtcNow,
            ValidatedByPersonId = validatorPersonId
        }).ToList();

        var saved = await attendanceRepository.SaveLessonCallAsync(
            organizationId, lessonId, validatorPersonId, records, cancellationToken);
        return saved?.Select(ToResponse).ToList();
    }

    private static void Validate(Guid organizationId, Guid clientId, Guid lessonId, string status)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("ClientId is required.");
        }

        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.");
        }

        ValidateStatus(status);
    }

    private static void ValidateStatus(string status)
    {
        if (!AllowedStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Status must be present, absent, justified_absence or late.");
        }
    }

    private static void ValidateValidator(Guid validatorPersonId)
    {
        if (validatorPersonId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A validator person context is required.");
        }
    }

    private static AttendanceRecordResponse ToResponse(AttendanceRecord record)
    {
        return new AttendanceRecordResponse
        {
            Id = record.Id,
            OrganizationId = record.OrganizationId,
            LessonId = record.LessonId,
            ClientId = record.ClientId,
            ClientName = record.ClientName,
            AttendanceDate = record.AttendanceDate,
            Status = record.Status,
            ValidatedAt = record.ValidatedAt,
            ValidatedByPersonId = record.ValidatedByPersonId,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}
