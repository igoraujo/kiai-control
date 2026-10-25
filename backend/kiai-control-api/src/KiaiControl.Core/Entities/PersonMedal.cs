namespace KiaiControl.Core.Entities;

public sealed class PersonMedal
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PersonId { get; set; }

    public Guid MedalId { get; set; }

    public DateTimeOffset AwardedAt { get; set; }

    public string? Reason { get; set; }
}