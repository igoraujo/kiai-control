using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Reports;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlReportRepository(IDbConnectionFactory dbConnectionFactory) : IReportRepository
{
    public async Task<DashboardReportData> GetDashboardAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<DashboardReportData>(ReportQueries.Dashboard, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to
        });
    }

    public async Task<IReadOnlyList<AttendanceReportData>> GetAttendanceAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<AttendanceReportData>(ReportQueries.Attendance, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to,
            ModalityId = modalityId
        });
        return items.ToList();
    }

    public async Task<FinancialReportData> GetFinancialAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<FinancialReportData>(ReportQueries.Financial, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to
        });
    }

    public async Task<IReadOnlyList<ClientPerformanceReportData>> GetClientPerformanceAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<ClientPerformanceReportData>(ReportQueries.ClientPerformance, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to,
            ModalityId = modalityId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return items.ToList();
    }

    public async Task<IReadOnlyList<GraduationReportData>> GetGraduationAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<GraduationReportData>(ReportQueries.Graduation, new
        {
            OrganizationId = organizationId,
            From = from,
            To = to,
            ModalityId = modalityId
        });
        return items.ToList();
    }
}