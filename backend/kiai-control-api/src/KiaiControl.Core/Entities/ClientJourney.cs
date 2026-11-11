namespace KiaiControl.Core.Entities;
public sealed class AttendanceTimelineItem
{
    public Guid LessonId { get; set; }
    public string ModalityName { get; set; } = "";
    public DateTimeOffset StartsAt { get; set; }
    public string Status { get; set; } = "";
}

public sealed class GraduationTimelineItem
{
    public string ModalityName { get; set; } = "";
    public string LevelName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string EventType { get; set; } = "";
}

public sealed class ClientMedalItem
{
    public string Name { get; set; } = "";
    public string? Reason { get; set; }
    public DateTimeOffset AwardedAt { get; set; }
}

public sealed class ClientRankingItem
{
    public string CycleName { get; set; } = "";
    public int Position { get; set; }
    public int Points { get; set; }
    public int StreakCount { get; set; }
}

public sealed record ClientJourney(PagedResult<AttendanceTimelineItem> Attendance, IReadOnlyList<GraduationTimelineItem> Graduations, IReadOnlyList<ClientMedalItem> Medals, IReadOnlyList<ClientRankingItem> Rankings, int PresentCount, int CompletedCount);
