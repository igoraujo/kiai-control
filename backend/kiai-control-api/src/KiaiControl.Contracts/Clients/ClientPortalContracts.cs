namespace KiaiControl.Contracts.Clients;

public sealed class ClientPortalProfileResponse
{
    public Guid ClientId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? PreferredName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public DateOnly? JoinedAt { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class ClientPortalLessonResponse
{
    public Guid Id { get; init; }

    public Guid ModalityId { get; init; }

    public string? ModalityName { get; init; }

    public string? BranchName { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset EndsAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? CheckInStatus { get; init; }
}
