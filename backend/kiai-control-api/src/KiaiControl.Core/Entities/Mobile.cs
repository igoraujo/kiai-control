namespace KiaiControl.Core.Entities;

public sealed class MobileAccount
{
    public string FullName { get; set; } = "";
    public string? PreferredName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool HideFromRanking { get; set; }
    public bool PushEnabled { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
}

public sealed record MobileMembership(string OrganizationName, string Timezone, Guid? ClientId, Guid? TeacherId, bool HasMartialArts);
public sealed record AttendanceRanking(string ClientName, int Position, int AttendanceCount, bool IsMe);
public sealed record AttendanceMonth(string Month, int AttendanceCount);
public sealed record GraduationProgress(string ModalityName, string BeltName, int DegreeNumber,
    string? NextLevelName, int? LessonsRemaining, bool RequiresManualEvaluation);
public sealed class MobileProgress
{
    public int Year { get; set; }
    public int TotalClasses { get; set; }
    public int YearClasses { get; set; }
    public int CurrentStreak { get; set; }
    public int? TotalPosition { get; set; }
    public int? YearPosition { get; set; }
    public IReadOnlyList<AttendanceMonth> Months { get; set; } = [];
    public IReadOnlyList<GraduationProgress> Graduations { get; set; } = [];
}

public sealed record PushDelivery(Guid NotificationId, Guid OrganizationId, Guid PersonId,
    string Title, string Body, string Token);
