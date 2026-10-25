namespace KiaiControl.Core.Entities;

public sealed class RankingEntry
{
    public Guid RankingCycleId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int Points { get; set; }

    public int AttendanceCount { get; set; }

    public int StreakCount { get; set; }

    public int Position { get; set; }
}