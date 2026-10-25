namespace KiaiControl.Core.Entities;

public sealed class GraduationEvent
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClientId { get; set; }

    public Guid ModalityId { get; set; }

    public Guid? FromLevelId { get; set; }

    public Guid ToLevelId { get; set; }

    public string EventType { get; set; } = "promotion";

    public DateOnly EventDate { get; set; }

    public Guid? TeacherId { get; set; }

    public string? Justification { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}