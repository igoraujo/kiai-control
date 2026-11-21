using System.ComponentModel.DataAnnotations;

namespace KiaiControl.Contracts.Mobile;

public sealed record MobileContextResponse(string OrganizationName, string Timezone,
    IReadOnlyList<string> Capabilities, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions,
    bool MustChangePassword);

public sealed class MobileAccountResponse
{
    public string FullName { get; init; } = "";
    public string? PreferredName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool HideFromRanking { get; init; }
    public bool PushEnabled { get; init; }
    public string? PhotoBase64 { get; init; }
    public string? PhotoContentType { get; init; }
}

public sealed class SaveMobileAccountRequest
{
    [Required, MaxLength(160)] public string FullName { get; set; } = "";
    [MaxLength(100)] public string? PreferredName { get; set; }
    [EmailAddress, MaxLength(180)] public string? Email { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    public bool HideFromRanking { get; set; }
    public bool PushEnabled { get; set; }
}

public sealed class SaveMobilePhotoRequest
{
    [Required, MaxLength(700000)] public string Base64 { get; set; } = "";
    [Required] public string ContentType { get; set; } = "image/jpeg";
}

public sealed class RegisterDeviceRequest
{
    [Required, MaxLength(200)] public string InstallationId { get; set; } = "";
    [Required, MaxLength(4096)] public string Token { get; set; } = "";
    [Required, RegularExpression("^(android|ios)$")] public string Platform { get; set; } = "";
}

public sealed record AttendanceRankingResponse(string ClientName, int Position, int AttendanceCount, bool IsMe);
public sealed record AttendanceMonthResponse(string Month, int AttendanceCount);
public sealed record GraduationProgressResponse(string ModalityName, string BeltName, int DegreeNumber,
    string? NextLevelName, int? LessonsRemaining, bool RequiresManualEvaluation);
public sealed record MobileProgressResponse(int Year, int TotalClasses, int YearClasses, int CurrentStreak,
    int? TotalPosition, int? YearPosition, IReadOnlyList<AttendanceMonthResponse> Months,
    IReadOnlyList<GraduationProgressResponse> Graduations);
