namespace KiaiControl.Core.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PersonId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Channel { get; set; } = "in_app";

    public string Status { get; set; } = "pending";

    public DateTimeOffset? ScheduledAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}