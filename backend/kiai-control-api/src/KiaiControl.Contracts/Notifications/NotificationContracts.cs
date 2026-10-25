namespace KiaiControl.Contracts.Notifications;

public sealed class CreateNotificationRequest
{
    public Guid PersonId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Channel { get; set; } = "in_app";

    public DateTimeOffset? ScheduledAt { get; set; }
}

public sealed class NotificationResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid PersonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Channel { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset? ScheduledAt { get; init; }

    public DateTimeOffset? SentAt { get; init; }

    public DateTimeOffset? ReadAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}