namespace KiaiControl.Repositories.Queries.Notifications;

public static class NotificationQueries
{
    public const string Insert = @"
        insert into notifications (
            id, organization_id, person_id, title, body, channel, status, scheduled_at, created_at
        )
        select
            @Id, p.organization_id, p.id, @Title, @Body, @Channel::notification_channel,
            case when @ScheduledAt is null then 'sent'::notification_status else 'pending'::notification_status end,
            @ScheduledAt, @CreatedAt
        from persons p
        where p.id = @PersonId
          and p.organization_id = @OrganizationId
          and p.deleted_at is null
        returning
            id,
            organization_id as OrganizationId,
            person_id as PersonId,
            title,
            body,
            channel::text as Channel,
            status::text as Status,
            scheduled_at as ScheduledAt,
            sent_at as SentAt,
            read_at as ReadAt,
            created_at as CreatedAt;";

    public const string ListByPerson = @"
        select
            id,
            organization_id as OrganizationId,
            person_id as PersonId,
            title,
            body,
            channel::text as Channel,
            status::text as Status,
            scheduled_at as ScheduledAt,
            sent_at as SentAt,
            read_at as ReadAt,
            created_at as CreatedAt
        from notifications
        where organization_id = @OrganizationId and person_id = @PersonId
        order by created_at desc
        offset @Offset rows fetch next @PageSize rows only;";

    public const string MarkAsRead = @"
        update notifications
        set read_at = now(), status = 'read'
        where organization_id = @OrganizationId
          and person_id = @PersonId
          and id = @NotificationId
          and read_at is null
        returning id;";
}