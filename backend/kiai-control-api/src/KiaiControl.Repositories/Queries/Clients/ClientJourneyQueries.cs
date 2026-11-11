namespace KiaiControl.Repositories.Queries.Clients;

public static class ClientJourneyQueries
{
    public const string Read = @"
        select exists (
            select 1
            from clients c
            join persons p on p.id = c.person_id
            where c.id = @ClientId
                and c.organization_id = @OrganizationId
                and p.deleted_at is null
        );

        select
            a.lesson_id as LessonId,
            m.name as ModalityName,
            l.starts_at as StartsAt,
            a.status::text as Status
        from attendances a
        join lessons l on l.id = a.lesson_id
            and l.organization_id = a.organization_id
        join modalities m on m.id = l.modality_id
            and m.organization_id = l.organization_id
        where a.organization_id = @OrganizationId
            and a.client_id = @ClientId
        order by
            l.starts_at desc,
            a.id
        offset @Offset
        limit @Size;

        select count(*)
        from attendances
        where organization_id = @OrganizationId
            and client_id = @ClientId;

        select
            m.name as ModalityName,
            level.name as LevelName,
            g.granted_at as EventDate,
            'current' as EventType
        from client_graduations g
        join modalities m on m.id = g.modality_id
            and m.organization_id = g.organization_id
        join graduation_levels level on level.id = g.current_level_id
        where g.organization_id = @OrganizationId
            and g.client_id = @ClientId
        union all
        select
            m.name,
            level.name,
            e.event_date,
            e.event_type::text
        from graduation_events e
        join modalities m on m.id = e.modality_id
            and m.organization_id = e.organization_id
        join graduation_levels level on level.id = e.to_level_id
        where e.organization_id = @OrganizationId
            and e.client_id = @ClientId
        order by EventDate desc
        limit 200;

        select
            m.name,
            pm.reason,
            pm.awarded_at as AwardedAt
        from person_medals pm
        join medals m on m.id = pm.medal_id
            and m.organization_id = pm.organization_id
        join clients c on c.person_id = pm.person_id
            and c.organization_id = pm.organization_id
        where pm.organization_id = @OrganizationId
            and c.id = @ClientId
        order by pm.awarded_at desc
        limit 200;

        select
            cycle.name as CycleName,
            entry.position,
            entry.points,
            entry.streak_count as StreakCount
        from ranking_entries entry
        join ranking_cycles cycle on cycle.id = entry.ranking_cycle_id
        where cycle.organization_id = @OrganizationId
            and entry.client_id = @ClientId
        order by cycle.starts_at desc
        limit 50;

        select
            count(*) filter(
                            where a.status in ('present', 'late'))::integer as PresentCount,
            count(*)::integer as CompletedCount
        from attendances a
        join lessons l on l.id = a.lesson_id
            and l.organization_id = a.organization_id
        where a.organization_id = @OrganizationId
            and a.client_id = @ClientId
            and l.status = 'completed';
        ";

    public const string GetGoalClientId = @"
        select client_id
        from fitness_goals
        where organization_id = @OrganizationId
            and id = @GoalId;
        ";
}
