using KiaiControl.Contracts.Attendance;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Attendance;

public sealed class AttendanceUseCase(IAttendanceRepository attendanceRepository)
{
    private static readonly string[] AllowedStatuses = ["present", "absent", "justified_absence", "late"];

    public async Task<AttendanceRecordResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        Validate(request.OrganizationId, request.ClientId, request.LessonId, request.Status);

        var attendance = new AttendanceRecord
        {
            OrganizationId = request.OrganizationId,
            LessonId = request.LessonId,
            ClientId = request.ClientId,
            AttendanceDate = request.AttendanceDate,
            Status = request.Status
        };

        var created = await attendanceRepository.CreateAsync(attendance, cancellationToken);

        return new AttendanceRecordResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            LessonId = created.LessonId,
            ClientId = created.ClientId,
            AttendanceDate = created.AttendanceDate,
            Status = created.Status,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
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
            AttendanceDate = record.AttendanceDate,
            Status = record.Status,
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
            AttendanceDate = attendance.AttendanceDate,
            Status = attendance.Status,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.UpdatedAt
        };
    }

    public async Task<AttendanceRecordResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken)
    {
        Validate(organizationId, request.ClientId, request.LessonId, request.Status);

        var attendance = new AttendanceRecord
        {
            Id = id,
            OrganizationId = organizationId,
            LessonId = request.LessonId,
            ClientId = request.ClientId,
            AttendanceDate = request.AttendanceDate,
            Status = request.Status
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
            AttendanceDate = updated.AttendanceDate,
            Status = updated.Status,
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

    public async Task<IReadOnlyList<AttendanceRecordResponse>?> SaveLessonCallAsync(
        Guid organizationId,
        Guid lessonId,
        SaveAttendanceCallRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || lessonId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and LessonId are required.");
        }

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
            Status = entry.Status.Trim().ToLowerInvariant()
        }).ToList();

        var saved = await attendanceRepository.SaveLessonCallAsync(organizationId, lessonId, records, cancellationToken);
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

    private static AttendanceRecordResponse ToResponse(AttendanceRecord record)
    {
        return new AttendanceRecordResponse
        {
            Id = record.Id,
            OrganizationId = record.OrganizationId,
            LessonId = record.LessonId,
            ClientId = record.ClientId,
            AttendanceDate = record.AttendanceDate,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}
