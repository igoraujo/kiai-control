using KiaiControl.Contracts.Common;

namespace KiaiControl.Contracts.Clients;
public sealed record AttendanceTimelineResponse(Guid LessonId, string ModalityName, DateTimeOffset StartsAt, string Status);
public sealed record GraduationTimelineResponse(string ModalityName, string LevelName, DateOnly EventDate, string EventType);
public sealed record ClientMedalResponse(string Name, string? Reason, DateTimeOffset AwardedAt);
public sealed record ClientRankingResponse(string CycleName, int Position, int Points, int StreakCount);
public sealed record ClientJourneyResponse(PagedResponse<AttendanceTimelineResponse> Attendance, IReadOnlyList<GraduationTimelineResponse> Graduations, IReadOnlyList<ClientMedalResponse> Medals, IReadOnlyList<ClientRankingResponse> Rankings, int PresentCount, int CompletedCount);
