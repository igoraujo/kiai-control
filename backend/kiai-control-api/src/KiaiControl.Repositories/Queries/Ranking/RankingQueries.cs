namespace KiaiControl.Repositories.Queries.Ranking;

public static class RankingQueries
{
    public const string CreateCycle = @"
        insert into ranking_cycles (
            id, organization_id, modality_id, name, starts_at, ends_at, status, created_at
        )
        select @Id, o.id, @ModalityId, @Name, @StartsAt, @EndsAt, 'open', @CreatedAt
        from organizations o
        where o.id = @OrganizationId
          and (@ModalityId is null or exists (
              select 1 from modalities m where m.id = @ModalityId and m.organization_id = o.id and m.is_active = true
          ))
        returning id, organization_id as OrganizationId, modality_id as ModalityId,
                  name, starts_at as StartsAt, ends_at as EndsAt, status::text as Status, created_at as CreatedAt;
        ";

    public const string ListCycles = @"
        select id, organization_id as OrganizationId, modality_id as ModalityId,
               name, starts_at as StartsAt, ends_at as EndsAt, status::text as Status, created_at as CreatedAt
        from ranking_cycles
        where organization_id = @OrganizationId
        order by starts_at desc, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string LockCycle = @"
        select id, organization_id as OrganizationId, modality_id as ModalityId,
               name, starts_at as StartsAt, ends_at as EndsAt, status::text as Status, created_at as CreatedAt
        from ranking_cycles
        where organization_id = @OrganizationId and id = @CycleId and status = 'open'
        for update;
        ";

    public const string DeleteCycleEntries = @"
        delete from ranking_entries where ranking_cycle_id = @CycleId;
        ";

    public const string Recalculate = @"
        with target_cycle as (
            select id, organization_id, modality_id, starts_at, ends_at
            from ranking_cycles
            where organization_id = @OrganizationId and id = @CycleId and status = 'open'
        ), lesson_states as (
            select c.id as client_id,
                   l.id as lesson_id,
                   l.starts_at,
                   (a.status in ('present', 'late')) as attended
            from target_cycle cycle
            join lessons l on l.organization_id = cycle.organization_id
                          and l.status = 'completed'
                          and l.starts_at::date between cycle.starts_at and cycle.ends_at
            join clients c on c.organization_id = cycle.organization_id and c.current_status = 'active'
            left join person_modalities assignment on assignment.person_id = c.person_id
                and assignment.modality_id = l.modality_id
            left join modalities modality on modality.id = l.modality_id
                and modality.organization_id = cycle.organization_id
                and modality.is_active = true
            left join attendances a on a.organization_id = cycle.organization_id
                                   and a.lesson_id = l.id and a.client_id = c.id
            where assignment.person_id is not null
              and modality.id is not null
              and (cycle.modality_id is null or l.modality_id = cycle.modality_id)
        ), sequenced as (
            select lesson_states.*,
                   row_number() over (partition by client_id order by starts_at, lesson_id) as sequence_number,
                   count(*) filter (where attended) over (
                       partition by client_id order by starts_at, lesson_id rows unbounded preceding
                   ) as attended_sequence,
                   sum(case when attended then 0 else 1 end) over (
                       partition by client_id order by starts_at, lesson_id rows unbounded preceding
                   ) as absence_groups
            from lesson_states
        ), grouped_streaks as (
                 select client_id, (sequence_number - attended_sequence) as streak_group, attended,
                   bool_and(attended) as group_is_attended,
                   count(*) filter (where attended)::integer as streak_length,
                   max(starts_at) as latest_lesson_at
            from sequenced
                 group by client_id, sequence_number - attended_sequence, attended
        ), latest_streak as (
            select distinct on (client_id) client_id,
                   case when group_is_attended then streak_length else 0 end as streak_count
            from grouped_streaks
            order by client_id, latest_lesson_at desc
        ), totals as (
            select client_id,
                   count(*) filter (where attended)::integer as attendance_count
            from lesson_states
            group by client_id
        ), ranked as (
            select gen_random_uuid() as id,
                   cycle.id as ranking_cycle_id,
                   c.id as client_id,
                   coalesce(totals.attendance_count, 0) as points,
                   coalesce(totals.attendance_count, 0) as attendance_count,
                   coalesce(latest_streak.streak_count, 0) as streak_count,
                   row_number() over (
                       order by coalesce(totals.attendance_count, 0) desc,
                                coalesce(latest_streak.streak_count, 0) desc,
                                lower(p.full_name), c.id
                   )::integer as position
            from target_cycle cycle
            join clients c on c.organization_id = cycle.organization_id and c.current_status = 'active'
            join persons p on p.id = c.person_id and p.deleted_at is null and p.hide_from_ranking = false
            left join totals on totals.client_id = c.id
            left join latest_streak on latest_streak.client_id = c.id
            where exists (
                select 1
                from person_modalities assignment
                join modalities modality on modality.id = assignment.modality_id
                    and modality.organization_id = cycle.organization_id
                    and modality.is_active = true
                where assignment.person_id = c.person_id
                  and (cycle.modality_id is null or assignment.modality_id = cycle.modality_id)
            )
        )
        insert into ranking_entries (id, ranking_cycle_id, client_id, points, attendance_count, streak_count, position, updated_at)
        select id, ranking_cycle_id, client_id, points, attendance_count, streak_count, position, now()
        from ranked
        returning ranking_cycle_id as RankingCycleId, client_id as ClientId,
                  points, attendance_count as AttendanceCount, streak_count as StreakCount, position as Position;
        ";

    public const string GetCurrent = @"
        select entry.ranking_cycle_id as RankingCycleId,
               entry.client_id as ClientId,
               person.full_name as ClientName,
               entry.points,
               entry.attendance_count as AttendanceCount,
               entry.streak_count as StreakCount,
               entry.position as Position
        from ranking_entries entry
        join ranking_cycles cycle on cycle.id = entry.ranking_cycle_id
        join clients client on client.id = entry.client_id and client.organization_id = cycle.organization_id
        join persons person on person.id = client.person_id and person.hide_from_ranking = false and person.deleted_at is null
        where cycle.organization_id = @OrganizationId
          and cycle.status = 'open'
          and @Today between cycle.starts_at and cycle.ends_at
        order by entry.position
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetByCycle = @"
        select entry.ranking_cycle_id as RankingCycleId,
               entry.client_id as ClientId,
               person.full_name as ClientName,
               entry.points,
               entry.attendance_count as AttendanceCount,
               entry.streak_count as StreakCount,
               entry.position as Position
        from ranking_entries entry
        join ranking_cycles cycle on cycle.id = entry.ranking_cycle_id
        join clients client on client.id = entry.client_id and client.organization_id = cycle.organization_id
        join persons person on person.id = client.person_id and person.hide_from_ranking = false and person.deleted_at is null
        where cycle.organization_id = @OrganizationId and cycle.id = @CycleId
        order by entry.position
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string CreateMedal = @"
        insert into medals (id, organization_id, code, name, description, points_bonus, is_active, created_at)
        values (@Id, @OrganizationId, @Code, @Name, @Description, @PointsBonus, true, @CreatedAt)
        returning id, organization_id as OrganizationId, code, name, description,
                  points_bonus as PointsBonus, is_active as IsActive, created_at as CreatedAt;
        ";

    public const string ListMedals = @"
        select id, organization_id as OrganizationId, code, name, description,
               points_bonus as PointsBonus, is_active as IsActive, created_at as CreatedAt
        from medals where organization_id = @OrganizationId and is_active = true order by name;
        ";

    public const string AwardMedal = @"
        insert into person_medals (id, organization_id, person_id, medal_id, awarded_at, reason)
        select @Id, p.organization_id, p.id, m.id, @AwardedAt, @Reason
        from persons p
        join medals m on m.id = @MedalId and m.organization_id = p.organization_id and m.is_active = true
        where p.id = @PersonId and p.organization_id = @OrganizationId and p.deleted_at is null
        returning id, organization_id as OrganizationId, person_id as PersonId,
                  medal_id as MedalId, awarded_at as AwardedAt, reason;
        ";

    public const string ListPersonMedals = @"
        select id, organization_id as OrganizationId, person_id as PersonId,
               medal_id as MedalId, awarded_at as AwardedAt, reason
        from person_medals
        where organization_id = @OrganizationId and person_id = @PersonId
        order by awarded_at desc;
        ";

    public const string ListClientMedals = @"
        select award.id, award.organization_id as OrganizationId, award.person_id as PersonId,
               award.medal_id as MedalId, award.awarded_at as AwardedAt, award.reason
        from person_medals award
        join clients client on client.person_id = award.person_id and client.organization_id = award.organization_id
        where award.organization_id = @OrganizationId and client.id = @ClientId
        order by award.awarded_at desc;
        ";
}