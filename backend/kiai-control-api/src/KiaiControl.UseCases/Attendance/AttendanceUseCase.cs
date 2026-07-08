using KiaiControl.Contracts.Attendance;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Attendance;

public sealed class AttendanceUseCase(IAttendanceRepository attendanceRepository)
{
    public async Task<AttendanceRecordResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var attendance = new AttendanceRecord
        {
            OrganizationId = request.OrganizationId,
            StudentId = request.StudentId,
            AttendanceDate = request.AttendanceDate,
            Status = request.Status
        };

        var created = await attendanceRepository.CreateAsync(attendance, cancellationToken);

        return new AttendanceRecordResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            StudentId = created.StudentId,
            AttendanceDate = created.AttendanceDate,
            Status = created.Status
        };
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var attendance = await attendanceRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        return attendance.Select(record => new AttendanceRecordResponse
        {
            Id = record.Id,
            OrganizationId = record.OrganizationId,
            StudentId = record.StudentId,
            AttendanceDate = record.AttendanceDate,
            Status = record.Status
        }).ToList();
    }
}
