namespace KiaiControl.Contracts.Reports;

public sealed class ReportPeriodRequest
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }
}

public sealed class DashboardReportResponse
{
    public int ActiveClients { get; init; }

    public int NewClients { get; init; }

    public int CanceledClients { get; init; }

    public int CompletedLessons { get; init; }

    public decimal AttendanceRate { get; init; }

    public decimal RevenueExpected { get; init; }

    public decimal RevenueReceived { get; init; }

    public decimal RevenueOverdue { get; init; }
}

public sealed class AttendanceReportRow
{
    public Guid ModalityId { get; init; }

    public string? ModalityName { get; init; }

    public int LessonsHeld { get; init; }

    public int AttendanceCount { get; init; }

    public int AbsenceCount { get; init; }

    public decimal AttendanceRate { get; init; }
}

public sealed class FinancialReportResponse
{
    public decimal AmountIssued { get; init; }

    public decimal AmountPaid { get; init; }

    public decimal AmountOutstanding { get; init; }

    public decimal AmountOverdue { get; init; }

    public int InvoicesIssued { get; init; }

    public int InvoicesOverdue { get; init; }
}

public sealed class ClientPerformanceReportRow
{
    public Guid ClientId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public int AttendanceCount { get; init; }

    public int LessonsAvailable { get; init; }

    public decimal AttendanceRate { get; init; }

    public decimal AmountPaid { get; init; }

    public int ActiveFitnessGoals { get; init; }
}

public sealed class GraduationReportRow
{
    public Guid ModalityId { get; init; }

    public string ModalityName { get; init; } = string.Empty;

    public int PromotionCount { get; init; }

    public DateOnly LatestPromotion { get; init; }
}