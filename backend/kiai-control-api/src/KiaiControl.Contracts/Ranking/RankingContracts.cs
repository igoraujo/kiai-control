namespace KiaiControl.Contracts.Ranking;

public sealed class CreateRankingCycleRequest
{
    public Guid? ModalityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly StartsAt { get; set; }

    public DateOnly EndsAt { get; set; }
}

public sealed class RankingCycleResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid? ModalityId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateOnly StartsAt { get; init; }

    public DateOnly EndsAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class RankingEntryResponse
{
    public Guid RankingCycleId { get; init; }

    public Guid ClientId { get; init; }

    public string ClientName { get; init; } = string.Empty;

    public int Points { get; init; }

    public int AttendanceCount { get; init; }

    public int StreakCount { get; init; }

    public int Position { get; init; }
}

public sealed class CreateMedalRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PointsBonus { get; set; }
}

public sealed class MedalResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int PointsBonus { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AwardMedalRequest
{
    public Guid PersonId { get; set; }

    public string? Reason { get; set; }
}

public sealed class PersonMedalResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid PersonId { get; init; }

    public Guid MedalId { get; init; }

    public DateTimeOffset AwardedAt { get; init; }

    public string? Reason { get; init; }
}