namespace KiaiControl.Repositories.Queries.Mobile;

public static class MobileQueries
{
    public const string Membership = @"
        select o.name as OrganizationName, coalesce(s.timezone, 'America/Sao_Paulo') as Timezone,
            c.id as ClientId, t.id as TeacherId,
            exists (select 1 from person_modalities pm join modalities m on m.id = pm.modality_id
                and m.organization_id = p.organization_id and m.is_active = true and m.type = 'martial_art'
                where pm.person_id = p.id) as HasMartialArts
        from persons p
        join organizations o on o.id = p.organization_id
        left join organization_settings s on s.organization_id = p.organization_id
        left join clients c on c.person_id = p.id and c.organization_id = p.organization_id and c.current_status = 'active'
        left join teachers t on t.person_id = p.id and t.organization_id = p.organization_id and t.current_status = 'active'
        where p.organization_id = @Org and p.id = @Person and p.deleted_at is null;
        ";

    public const string Account = @"
        select p.full_name as FullName, p.preferred_name as PreferredName, p.email, p.phone,
            p.hide_from_ranking as HideFromRanking, p.profile_photo as Photo,
            p.profile_photo_content_type as PhotoContentType, coalesce(pref.push_enabled, false) as PushEnabled
        from persons p
        left join mobile_preferences pref on pref.organization_id = p.organization_id and pref.person_id = p.id
        where p.organization_id = @Org and p.id = @Person and p.deleted_at is null;
        ";

    public const string SaveAccount = @"
        with saved as (
            update persons set full_name = @FullName, preferred_name = @PreferredName, email = @Email,
                phone = @Phone, hide_from_ranking = @HideFromRanking, updated_at = now()
            where organization_id = @Org and id = @Person and deleted_at is null
            returning id, organization_id
        )
        insert into mobile_preferences (organization_id, person_id, push_enabled)
        select organization_id, id, @PushEnabled from saved
        on conflict (organization_id, person_id) do update set push_enabled = excluded.push_enabled;
        ";

    public const string SavePhoto = @"
        update persons set profile_photo = @Photo, profile_photo_content_type = @ContentType, updated_at = now()
        where organization_id = @Org and id = @Person and deleted_at is null;
        ";

    public const string Progress = @"
        with confirmed as (
            select l.starts_at, s.timezone
            from attendances a
            join lessons l on l.id = a.lesson_id and l.organization_id = a.organization_id and l.status = 'completed'
            join clients c on c.id = a.client_id and c.organization_id = a.organization_id
            left join organization_settings s on s.organization_id = a.organization_id
            where a.organization_id = @Org and a.client_id = @Client
                and a.status in ('present', 'late') and a.validated_at is not null
                and a.validated_by_person_id <> c.person_id
        )
        select extract(year from now() at time zone coalesce((select timezone from organization_settings where organization_id = @Org), 'America/Sao_Paulo'))::integer as Year,
            count(1)::integer as TotalClasses,
            count(1) filter (where extract(year from starts_at at time zone coalesce(timezone, 'America/Sao_Paulo')) =
                extract(year from now() at time zone coalesce(timezone, 'America/Sao_Paulo')))::integer as YearClasses
        from confirmed;

        with sessions as (
            select l.id, l.starts_at,
                coalesce(a.status in ('present', 'late') and a.validated_at is not null and a.validated_by_person_id <> c.person_id, false) as attended
            from clients c
            join lessons l on l.organization_id = c.organization_id and l.status = 'completed'
            join person_modalities pm on pm.person_id = c.person_id and pm.modality_id = l.modality_id
            left join attendances a on a.lesson_id = l.id and a.client_id = c.id and a.organization_id = c.organization_id
            where c.organization_id = @Org and c.id = @Client
                and l.starts_at >= coalesce(c.joined_at::timestamptz, c.created_at)
        ), grouped as (
            select attended, sum(case when attended then 0 else 1 end) over (order by starts_at desc, id desc) as missed
            from sessions
        )
        select count(1)::integer from grouped where attended and missed = 0;

        select to_char(l.starts_at at time zone coalesce(s.timezone, 'America/Sao_Paulo'), 'YYYY-MM') as Month,
            count(1)::integer as AttendanceCount
        from attendances a
        join lessons l on l.id = a.lesson_id and l.organization_id = a.organization_id and l.status = 'completed'
        join clients c on c.id = a.client_id and c.organization_id = a.organization_id
        left join organization_settings s on s.organization_id = a.organization_id
        where a.organization_id = @Org and a.client_id = @Client and a.status in ('present', 'late')
            and a.validated_at is not null and a.validated_by_person_id <> c.person_id
            and l.starts_at >= now() - interval '12 months'
        group by Month order by Month;

        select m.name as ModalityName, b.name as BeltName, current_level.degree_number as DegreeNumber,
            next_level.name as NextLevelName,
            case when next_level.id is null then null else greatest(0, next_level.min_lessons - stats.attended)::integer end as LessonsRemaining,
            coalesce(next_level.requires_manual_evaluation, false) as RequiresManualEvaluation
        from client_graduations cg
        join clients c on c.id = cg.client_id and c.organization_id = cg.organization_id
        join modalities m on m.id = cg.modality_id and m.organization_id = cg.organization_id and m.type = 'martial_art'
        join graduation_levels current_level on current_level.id = cg.current_level_id
        join graduation_belts b on b.id = current_level.belt_id
        left join lateral (
            select level.id, level.name, level.min_lessons, level.requires_manual_evaluation
            from graduation_levels level
            where level.graduation_system_id = current_level.graduation_system_id and level.hierarchy_order > current_level.hierarchy_order
            order by level.hierarchy_order limit 1
        ) next_level on true
        left join lateral (
            select count(1)::integer as attended from attendances a
            join lessons l on l.id = a.lesson_id and l.organization_id = a.organization_id and l.status = 'completed'
            where a.organization_id = cg.organization_id and a.client_id = cg.client_id and l.modality_id = cg.modality_id
                and l.starts_at::date > cg.granted_at
                and a.status in ('present', 'late') and a.validated_at is not null and a.validated_by_person_id <> c.person_id
        ) stats on true
        where cg.organization_id = @Org and cg.client_id = @Client order by m.name;
        ";

    public const string Ranking = @"
        with totals as (
            select c.id, coalesce(nullif(p.preferred_name, ''), p.full_name) as ClientName,
                count(1) filter (where a.id is not null)::integer as AttendanceCount
            from clients c
            join persons p on p.id = c.person_id and p.organization_id = c.organization_id
                and p.deleted_at is null and p.hide_from_ranking = false
            left join organization_settings s on s.organization_id = c.organization_id
            left join attendances a on a.client_id = c.id and a.organization_id = c.organization_id
                and a.status in ('present', 'late') and a.validated_at is not null and a.validated_by_person_id <> c.person_id
                and exists (
                    select 1 from lessons l where l.id = a.lesson_id and l.organization_id = a.organization_id and l.status = 'completed'
                        and (not @YearOnly or extract(year from l.starts_at at time zone coalesce(s.timezone, 'America/Sao_Paulo')) =
                            extract(year from now() at time zone coalesce(s.timezone, 'America/Sao_Paulo')))
                )
            where c.organization_id = @Org and c.current_status = 'active'
            group by c.id, p.preferred_name, p.full_name
        ), ranked as (
            select ClientName, rank() over (order by AttendanceCount desc)::integer as Position, AttendanceCount, id = @Client as IsMe, id
            from totals
        )
        select ClientName, Position, AttendanceCount, IsMe from ranked
        where not @MineOnly or IsMe
        order by Position, lower(ClientName), id offset @Offset limit @Size;
        ";

    public const string RegisterDevice = @"
        insert into mobile_devices (installation_id, organization_id, person_id, token, session_version, platform, updated_at)
        select @Installation, @Org, p.id, @Token, account.session_version, @Platform, now() from persons p
        join user_accounts account on account.person_id = p.id and account.is_active = true
        where p.organization_id = @Org and p.id = @Person and p.deleted_at is null
        on conflict (installation_id) do update set organization_id = excluded.organization_id, person_id = excluded.person_id,
            token = excluded.token, session_version = excluded.session_version, platform = excluded.platform, updated_at = now();
        ";

    public const string RemoveDevice = @"
        delete from mobile_devices where organization_id = @Org and person_id = @Person and installation_id = @Installation;
        ";
}
