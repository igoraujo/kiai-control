using KiaiControl.Contracts.Reports;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Reports;

public sealed class ReportUseCase(IReportRepository repository)
{
    public async Task<DashboardReportResponse> DashboardAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        Validate(organizationId, from, to);
        var data = await repository.GetDashboardAsync(organizationId, from, to, cancellationToken);
        return new DashboardReportResponse
        {
            ActiveClients = data.ActiveClients,
            NewClients = data.NewClients,
            CanceledClients = data.CanceledClients,
            CompletedLessons = data.CompletedLessons,
            AttendanceRate = data.AttendanceRate,
            RevenueExpected = data.RevenueExpected,
            RevenueReceived = data.RevenueReceived,
            RevenueOverdue = data.RevenueOverdue
        };
    }

    public async Task<IReadOnlyList<AttendanceReportRow>> AttendanceAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, from, to);
        var rows = await repository.GetAttendanceAsync(organizationId, from, to, modalityId, cancellationToken);
        return rows.Select(row => new AttendanceReportRow
        {
            ClassGroupId = row.ClassGroupId,
            ClassGroupName = row.ClassGroupName,
            LessonsHeld = row.LessonsHeld,
            AttendanceCount = row.AttendanceCount,
            AbsenceCount = row.AbsenceCount,
            AttendanceRate = row.AttendanceRate
        }).ToList();
    }

    public async Task<FinancialReportResponse> FinancialAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        Validate(organizationId, from, to);
        var data = await repository.GetFinancialAsync(organizationId, from, to, cancellationToken);
        return new FinancialReportResponse
        {
            AmountIssued = data.AmountIssued,
            AmountPaid = data.AmountPaid,
            AmountOutstanding = data.AmountOutstanding,
            AmountOverdue = data.AmountOverdue,
            InvoicesIssued = data.InvoicesIssued,
            InvoicesOverdue = data.InvoicesOverdue
        };
    }

    public async Task<IReadOnlyList<ClientPerformanceReportRow>> ClientPerformanceAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, from, to);
        var rows = await repository.GetClientPerformanceAsync(organizationId, from, to, modalityId, page, pageSize, cancellationToken);
        return rows.Select(row => new ClientPerformanceReportRow
        {
            ClientId = row.ClientId,
            FullName = row.FullName,
            AttendanceCount = row.AttendanceCount,
            LessonsAvailable = row.LessonsAvailable,
            AttendanceRate = row.AttendanceRate,
            AmountPaid = row.AmountPaid,
            ActiveFitnessGoals = row.ActiveFitnessGoals
        }).ToList();
    }

    public async Task<IReadOnlyList<GraduationReportRow>> GraduationAsync(
        Guid organizationId,
        DateOnly from,
        DateOnly to,
        Guid? modalityId,
        CancellationToken cancellationToken)
    {
        Validate(organizationId, from, to);
        var rows = await repository.GetGraduationAsync(organizationId, from, to, modalityId, cancellationToken);
        return rows.Select(row => new GraduationReportRow
        {
            ModalityId = row.ModalityId,
            ModalityName = row.ModalityName,
            PromotionCount = row.PromotionCount,
            LatestPromotion = row.LatestPromotion
        }).ToList();
    }

    private static void Validate(Guid organizationId, DateOnly from, DateOnly to)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (from == default || to < from)
        {
            throw new ArgumentException("The report period is invalid.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            throw new ArgumentException("Report periods cannot exceed 367 days.");
        }
    }
}