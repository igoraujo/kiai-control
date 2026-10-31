namespace KiaiControl.Core.Entities;

public sealed class DashboardReportData
{
    public int ActiveClients { get; set; }

    public int NewClients { get; set; }

    public int CanceledClients { get; set; }

    public int CompletedLessons { get; set; }

    public decimal AttendanceRate { get; set; }

    public decimal RevenueExpected { get; set; }

    public decimal RevenueReceived { get; set; }

    public decimal RevenueOverdue { get; set; }
}

public sealed class AttendanceReportData
{
    public Guid ModalityId { get; set; }

    public string? ModalityName { get; set; }

    public int LessonsHeld { get; set; }

    public int AttendanceCount { get; set; }

    public int AbsenceCount { get; set; }

    public decimal AttendanceRate { get; set; }
}

public sealed class FinancialReportData
{
    public decimal AmountIssued { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountOutstanding { get; set; }

    public decimal AmountOverdue { get; set; }

    public int InvoicesIssued { get; set; }

    public int InvoicesOverdue { get; set; }
}

public sealed class ClientPerformanceReportData
{
    public Guid ClientId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int AttendanceCount { get; set; }

    public int LessonsAvailable { get; set; }

    public decimal AttendanceRate { get; set; }

    public decimal AmountPaid { get; set; }

    public int ActiveFitnessGoals { get; set; }
}

public sealed class GraduationReportData
{
    public Guid ModalityId { get; set; }

    public string ModalityName { get; set; } = string.Empty;

    public int PromotionCount { get; set; }

    public DateOnly LatestPromotion { get; set; }
}