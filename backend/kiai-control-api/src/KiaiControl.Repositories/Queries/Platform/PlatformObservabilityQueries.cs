namespace KiaiControl.Repositories.Queries.Platform;

public static class PlatformObservabilityQueries
{
    public const string LockRetentionOrganizations = """
        select id from organizations order by id for share;
        """;

    public const string Dashboard = """
        select
            coalesce(sum(m.requests), 0)::bigint as Requests,
            coalesce(sum(m.requests) filter (where m.status_code >= 500), 0)::bigint as ServerErrors,
            coalesce(sum(m.requests) filter (where m.status_code between 400 and 499), 0)::bigint as ClientErrors,
            coalesce(sum(m.duration_total_ms) / nullif(sum(m.requests), 0), 0) as AverageDurationMs,
            coalesce(max(m.max_duration_ms), 0) as MaxDurationMs,
            controls.Active,
            controls.Suspended,
            controls.Inactive,
            controls.Archived,
            controls.PrivacyHeld,
            controls.WithoutActiveOwner
        from (
            select
                count(1) filter (where o.deleted_at is null and o.status = 'active')::int as Active,
                count(1) filter (where o.deleted_at is null and o.status = 'suspended')::int as Suspended,
                count(1) filter (where o.deleted_at is null and o.status = 'inactive')::int as Inactive,
                count(1) filter (where o.deleted_at is not null)::int as Archived,
                count(1) filter (where o.privacy_hold)::int as PrivacyHeld,
                count(1) filter (where o.deleted_at is null and not exists (
                    select 1
                    from persons p
                    join user_accounts u on u.person_id = p.id
                    join person_roles pr on pr.person_id = p.id
                    join roles r on r.id = pr.role_id
                    where p.organization_id = o.id and p.deleted_at is null
                        and p.status = 'active' and u.is_active and r.code = 'owner'
                ))::int as WithoutActiveOwner
            from organizations o
            where o.id <> @PlatformOrganizationId
                and (@OrganizationId is null or o.id = @OrganizationId)
        ) controls
        left join platform_request_metrics m
            on m.bucket_start >= @From and m.bucket_start < @To
            and (@OrganizationId is null or m.organization_id = @OrganizationId)
        group by controls.Active, controls.Suspended, controls.Inactive, controls.Archived,
            controls.PrivacyHeld, controls.WithoutActiveOwner;

        select
            days.day as Day,
            coalesce(sum(m.requests), 0)::bigint as Requests,
            coalesce(sum(m.requests) filter (where m.status_code >= 500), 0)::bigint as ServerErrors,
            coalesce(sum(m.duration_total_ms) / nullif(sum(m.requests), 0), 0) as AverageDurationMs
        from generate_series(
            date_trunc('day', @From::timestamptz, 'UTC'),
            date_trunc('day', @To::timestamptz - interval '1 microsecond', 'UTC'),
            interval '1 day'
        ) days(day)
        left join platform_request_metrics m
            on date_trunc('day', m.bucket_start, 'UTC') = days.day
            and m.bucket_start >= @From and m.bucket_start < @To
            and (@OrganizationId is null or m.organization_id = @OrganizationId)
        group by days.day
        order by days.day;

        select
            o.id,
            o.name,
            o.status::text as Status,
            o.deleted_at is not null as Archived,
            o.privacy_hold as PrivacyHold,
            accounts.Accounts,
            accounts.ActiveOwners,
            accounts.BlockedAccounts,
            accounts.PendingPasswords,
            coalesce(traffic.Requests, 0)::bigint as Requests,
            coalesce(traffic.ServerErrors, 0)::bigint as ServerErrors,
            (select count(1)::int from platform_incidents i
                where i.organization_id = o.id and i.created_at >= @From and i.created_at < @To) as Incidents,
            (select max(a.created_at) from audit_logs a where a.organization_id = o.id) as LastActivityAt
        from organizations o
        cross join lateral (
            select
                count(1)::int as Accounts,
                count(1) filter (where u.is_active and p.status = 'active' and exists (
                    select 1 from person_roles pr join roles r on r.id = pr.role_id
                    where pr.person_id = p.id and r.code = 'owner'
                ))::int as ActiveOwners,
                count(1) filter (where not u.is_active)::int as BlockedAccounts,
                count(1) filter (where u.must_change_password)::int as PendingPasswords
            from user_accounts u
            join persons p on p.id = u.person_id
            where p.organization_id = o.id and p.deleted_at is null and not u.is_platform_owner
        ) accounts
        left join lateral (
            select
                sum(m.requests) as Requests,
                sum(m.requests) filter (where m.status_code >= 500) as ServerErrors
            from platform_request_metrics m
            where m.organization_id = o.id and m.bucket_start >= @From and m.bucket_start < @To
        ) traffic on true
        where o.id <> @PlatformOrganizationId
            and (@OrganizationId is null or o.id = @OrganizationId)
        order by coalesce(traffic.ServerErrors, 0) desc, lower(o.name), o.id
        offset @Offset
        limit @PageSize;

        select count(1)::int
        from organizations
        where id <> @PlatformOrganizationId
            and (@OrganizationId is null or id = @OrganizationId);

        select
            m.route,
            m.method,
            sum(m.requests)::bigint as Requests,
            coalesce(sum(m.requests) filter (where m.status_code >= 500), 0)::bigint as ServerErrors,
            sum(m.duration_total_ms) / nullif(sum(m.requests), 0) as AverageDurationMs
        from platform_request_metrics m
        where m.bucket_start >= @From and m.bucket_start < @To
            and (@OrganizationId is null or m.organization_id = @OrganizationId)
        group by m.route, m.method
        order by ServerErrors desc, Requests desc, m.route, m.method
        limit 20;
        """;

    public const string UpsertMetric = """
        insert into platform_request_metrics (
            bucket_start, organization_id, route, method, status_code,
            requests, duration_total_ms, max_duration_ms
        )
        select @BucketStart, @OrganizationId, @Route, @Method, @StatusCode,
            @Requests, @DurationTotalMs, @MaxDurationMs
        where @OrganizationId::uuid is null or exists (select 1 from organizations where id = @OrganizationId)
        on conflict (bucket_start, organization_id, route, method, status_code) do update
        set
            requests = platform_request_metrics.requests + excluded.requests,
            duration_total_ms = platform_request_metrics.duration_total_ms + excluded.duration_total_ms,
            max_duration_ms = greatest(platform_request_metrics.max_duration_ms, excluded.max_duration_ms);
        """;

    public const string Retention = """
        with candidates as (
            select m.id from platform_request_metrics m
            left join organizations o on o.id = m.organization_id
            where m.bucket_start < @MetricsBefore and not coalesce(o.privacy_hold, false)
            order by m.bucket_start limit 2000
        ), removed as (
            delete from platform_request_metrics where id in (select id from candidates) returning id
        )
        select count(1)::int from removed;

        with candidates as (
            select i.id from platform_incidents i
            left join organizations o on o.id = i.organization_id
            where i.created_at < @IncidentsBefore and not coalesce(o.privacy_hold, false)
            order by i.created_at limit 2000
        ), removed as (
            delete from platform_incidents where id in (select id from candidates) returning id
        )
        select count(1)::int from removed;

        with candidates as (
            select a.id from audit_logs a
            join organizations o on o.id = a.organization_id
            where a.created_at < @AuditBefore and a.personal_data_redacted_at is null
                and not o.privacy_hold
            order by a.created_at limit 2000
        ), redacted as (
            update audit_logs
            set
                actor_person_id = null,
                actor_user_account_id = null,
                entity_id = null,
                ip_address = null,
                user_agent = null,
                metadata = '{"personalDataRedacted":true}'::jsonb,
                personal_data_redacted_at = now()
            where id in (select id from candidates)
            returning id
        )
        select count(1)::int from redacted;
        """;

    public const string RecordRetention = """
        insert into audit_logs (id, organization_id, action, entity_name, metadata, created_at)
        select @Id, id, 'platform.privacy.retention_applied', 'platform.privacy', @Metadata::jsonb, now()
        from organizations where id = @PlatformOrganizationId;
        """;
}
