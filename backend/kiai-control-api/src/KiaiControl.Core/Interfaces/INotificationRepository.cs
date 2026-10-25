using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListByPersonAsync(
        Guid organizationId,
        Guid personId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(Guid organizationId, Guid personId, Guid notificationId, CancellationToken cancellationToken = default);
}