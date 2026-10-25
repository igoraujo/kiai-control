using KiaiControl.Contracts.Notifications;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Notifications;

public sealed class NotificationUseCase(INotificationRepository notificationRepository)
{
    private static readonly string[] AllowedChannels = ["in_app", "push", "email"];

    public async Task<NotificationResponse?> CreateAsync(
        Guid organizationId,
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || request.PersonId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and PersonId are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160
            || string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 10000)
        {
            throw new ArgumentException("Title and Body are required and must be within their maximum lengths.");
        }

        var channel = AllowedChannels.FirstOrDefault(value => string.Equals(value, request.Channel.Trim(), StringComparison.OrdinalIgnoreCase));
        if (channel is null)
        {
            throw new ArgumentException("Channel must be in_app, push or email.");
        }

        if (request.ScheduledAt is not null && request.ScheduledAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("ScheduledAt must be in the future.");
        }

        var notification = await notificationRepository.CreateAsync(new Notification
        {
            OrganizationId = organizationId,
            PersonId = request.PersonId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Channel = channel,
            ScheduledAt = request.ScheduledAt
        }, cancellationToken);
        return notification is null ? null : ToResponse(notification);
    }

    public async Task<IReadOnlyList<NotificationResponse>> ListMineAsync(
        Guid organizationId,
        Guid personId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        var notifications = await notificationRepository.ListByPersonAsync(organizationId, personId, page, pageSize, cancellationToken);
        return notifications.Select(ToResponse).ToList();
    }

    public Task<bool> MarkMineAsReadAsync(
        Guid organizationId,
        Guid personId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("NotificationId is required.");
        }

        return notificationRepository.MarkAsReadAsync(organizationId, personId, notificationId, cancellationToken);
    }

    private static void EnsureIds(Guid organizationId, Guid personId)
    {
        if (organizationId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and PersonId are required.");
        }
    }

    private static NotificationResponse ToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            OrganizationId = notification.OrganizationId,
            PersonId = notification.PersonId,
            Title = notification.Title,
            Body = notification.Body,
            Channel = notification.Channel,
            Status = notification.Status,
            ScheduledAt = notification.ScheduledAt,
            SentAt = notification.SentAt,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}