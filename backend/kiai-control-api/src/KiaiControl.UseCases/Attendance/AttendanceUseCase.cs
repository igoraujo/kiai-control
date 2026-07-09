using KiaiControl.Contracts.Attendance;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Attendance;

public sealed class AttendanceUseCase(IAttendanceRepository attendanceRepository)
{
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

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }
    }
}
