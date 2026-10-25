using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IReportRepository
{
    Task<DashboardReportData> GetDashboardAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceReportData>> GetAttendanceAsync(Guid organizationId, DateOnly from, DateOnly to, Guid? modalityId, CancellationToken cancellationToken = default);

    Task<FinancialReportData> GetFinancialAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientPerformanceReportData>> GetClientPerformanceAsync(Guid organizationId, DateOnly from, DateOnly to, Guid? modalityId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraduationReportData>> GetGraduationAsync(Guid organizationId, DateOnly from, DateOnly to, Guid? modalityId, CancellationToken cancellationToken = default);
}