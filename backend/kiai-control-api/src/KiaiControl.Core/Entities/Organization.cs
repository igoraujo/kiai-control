namespace KiaiControl.Core.Entities;

public sealed class Organization
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? TradeName { get; init; }

    public string? Email { get; init; }

    public string Status { get; init; } = "active";

    public DateTimeOffset CreatedAt { get; init; }
}
