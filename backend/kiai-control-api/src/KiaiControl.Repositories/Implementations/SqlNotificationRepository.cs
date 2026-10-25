using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Notifications;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlNotificationRepository(IDbConnectionFactory dbConnectionFactory) : INotificationRepository
{
    public async Task<Notification?> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.Id = Guid.NewGuid();
        notification.CreatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Notification>(NotificationQueries.Insert, notification);
    }

    public async Task<IReadOnlyList<Notification>> ListByPersonAsync(
        Guid organizationId,
        Guid personId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<Notification>(NotificationQueries.ListByPerson, new
        {
            OrganizationId = organizationId,
            PersonId = personId,
            Offset = (safePage - 1) * safePageSize,
            PageSize = safePageSize
        });
        return items.ToList();
    }

    public async Task<bool> MarkAsReadAsync(Guid organizationId, Guid personId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(NotificationQueries.MarkAsRead, new
        {
            OrganizationId = organizationId,
            PersonId = personId,
            NotificationId = notificationId
        });
        return affected > 0;
    }
}