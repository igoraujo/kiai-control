namespace KiaiControl.Repositories.Queries.Notifications;

public static class PushDeliveryQueries
{
    public const string FailExhausted = @"
        update notifications n set status = 'failed'
        where n.channel = 'push' and n.status = 'pending'
            and exists (select 1 from push_deliveries d where d.notification_id = n.id)
            and not exists (select 1 from push_deliveries d where d.notification_id = n.id and (d.accepted_at is not null or d.attempts < 5 or d.available_at > now()));
        ";

    public const string Enqueue = @"
        insert into push_deliveries (notification_id, installation_id)
        select n.id, d.installation_id from notifications n
        join mobile_preferences p on p.organization_id = n.organization_id and p.person_id = n.person_id and p.push_enabled
        join mobile_devices d on d.organization_id = n.organization_id and d.person_id = n.person_id
        join user_accounts account on account.person_id = d.person_id and account.is_active = true and account.session_version = d.session_version
        join persons person on person.id = n.person_id and person.organization_id = n.organization_id and person.deleted_at is null
        join organizations o on o.id = n.organization_id and o.status = 'active'
        where n.channel = 'push' and n.status = 'pending' and coalesce(n.scheduled_at, n.created_at) <= now()
            and coalesce(n.scheduled_at, n.created_at) >= now() - interval '30 days'
        on conflict do nothing;
        ";

    public const string Claim = @"
        with due as (
            select pd.notification_id, pd.installation_id from push_deliveries pd
            join notifications n on n.id = pd.notification_id
            join mobile_devices d on d.installation_id = pd.installation_id and d.organization_id = n.organization_id and d.person_id = n.person_id
            join mobile_preferences p on p.organization_id = n.organization_id and p.person_id = n.person_id and p.push_enabled
            join organizations o on o.id = n.organization_id and o.status = 'active'
            join user_accounts account on account.person_id = d.person_id and account.is_active = true and account.session_version = d.session_version
            join persons person on person.id = n.person_id and person.organization_id = n.organization_id and person.deleted_at is null
            where pd.accepted_at is null and pd.attempts < 5 and pd.available_at <= now() and n.status <> 'read'
            order by pd.available_at limit 50 for update of pd skip locked
        ), claimed as (
            update push_deliveries pd set attempts = pd.attempts + 1, available_at = now() + interval '2 minutes'
            from due where pd.notification_id = due.notification_id and pd.installation_id = due.installation_id
            returning pd.notification_id, pd.installation_id
        )
        select n.id as NotificationId, n.organization_id as OrganizationId, n.person_id as PersonId, n.title, n.body, d.token
        from claimed c join notifications n on n.id = c.notification_id
        join mobile_devices d on d.installation_id = c.installation_id;
        ";

    public const string Accept = @"
        update push_deliveries pd set accepted_at = now()
        from mobile_devices d where pd.installation_id = d.installation_id and pd.notification_id = @NotificationId
            and d.organization_id = @OrganizationId and d.person_id = @PersonId and d.token = @Token;
        update notifications set sent_at = coalesce(sent_at, now()), status = case when read_at is null then 'sent'::notification_status else status end
        where id = @NotificationId and organization_id = @OrganizationId and person_id = @PersonId;
        ";

    public const string RemoveInvalidToken = @"
        delete from mobile_devices where organization_id = @OrganizationId and person_id = @PersonId and token = @Token;
        ";
}
