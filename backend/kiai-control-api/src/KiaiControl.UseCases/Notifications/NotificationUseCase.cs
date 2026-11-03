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
            throw new ArgumentException(SystemMessages.OrganizationIdAndPersonIdAreRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160
            || string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 10000)
        {
            throw new ArgumentException(SystemMessages.TitleAndBodyAreRequiredAndMustBeWithinTheirMaximumLengths);
        }

        var channel = AllowedChannels.FirstOrDefault(value => string.Equals(value, request.Channel.Trim(), StringComparison.OrdinalIgnoreCase));
        if (channel is null)
        {
            throw new ArgumentException(SystemMessages.ChannelMustBeInAppPushOrEmail);
        }

        if (request.ScheduledAt is not null && request.ScheduledAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(SystemMessages.ScheduledAtMustBeInTheFuture);
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

    public async Task<IReadOnlyList<NotificationResponse>> ListOrganizationAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }

        var notifications = await notificationRepository.ListByOrganizationAsync(
            organizationId, page, pageSize, cancellationToken);
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
            throw new ArgumentException(SystemMessages.NotificationIdIsRequired);
        }

        return notificationRepository.MarkAsReadAsync(organizationId, personId, notificationId, cancellationToken);
    }

    private static void EnsureIds(Guid organizationId, Guid personId)
    {
        if (organizationId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndPersonIdAreRequired);
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