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
