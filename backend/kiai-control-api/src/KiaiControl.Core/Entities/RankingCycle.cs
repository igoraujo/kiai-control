namespace KiaiControl.Core.Entities;

public sealed class RankingCycle
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? ModalityId { get; set; }

    public Guid? ClassGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly StartsAt { get; set; }

    public DateOnly EndsAt { get; set; }

    public string Status { get; set; } = "open";

    public DateTimeOffset CreatedAt { get; set; }
}