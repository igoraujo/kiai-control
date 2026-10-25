namespace KiaiControl.Repositories.Queries.Progression;

public static class ProgressionQueries
{
    public const string CreateGraduationSystem = @"
        insert into graduation_systems (id, organization_id, modality_id, name, description, is_active, created_at)
        select @Id, m.organization_id, m.id, @Name, @Description, true, @CreatedAt
        from modalities m
        where m.id = @ModalityId and m.organization_id = @OrganizationId and m.is_active = true
        returning id, organization_id as OrganizationId, modality_id as ModalityId, name, description, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string ListGraduationSystems = @"
        select id, organization_id as OrganizationId, modality_id as ModalityId, name, description, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
        from graduation_systems
        where organization_id = @OrganizationId
        order by name;
        ";

    public const string CreateGraduationLevel = @"
        insert into graduation_levels (
            id, graduation_system_id, name, hierarchy_order, min_training_days,
            min_attendance_percentage, min_lessons, min_age, requires_manual_evaluation, created_at
        )
        select @Id, gs.id, @Name, @HierarchyOrder, @MinTrainingDays,
               @MinAttendancePercentage, @MinLessons, @MinAge, @RequiresManualEvaluation, @CreatedAt
        from graduation_systems gs
        where gs.id = @GraduationSystemId and gs.organization_id = @OrganizationId and gs.is_active = true
        returning id, graduation_system_id as GraduationSystemId, name, hierarchy_order as HierarchyOrder,
                  min_training_days as MinTrainingDays, min_attendance_percentage as MinAttendancePercentage,
                  min_lessons as MinLessons, min_age as MinAge, requires_manual_evaluation as RequiresManualEvaluation,
                  created_at as CreatedAt;
        ";

    public const string ListGraduationLevels = @"
        select l.id, l.graduation_system_id as GraduationSystemId, l.name, l.hierarchy_order as HierarchyOrder,
               l.min_training_days as MinTrainingDays, l.min_attendance_percentage as MinAttendancePercentage,
               l.min_lessons as MinLessons, l.min_age as MinAge, l.requires_manual_evaluation as RequiresManualEvaluation,
               l.created_at as CreatedAt
        from graduation_levels l
        join graduation_systems gs on gs.id = l.graduation_system_id
        where gs.organization_id = @OrganizationId and gs.id = @SystemId
        order by l.hierarchy_order;
        ";

    public const string SetClientGraduation = @"
        insert into client_graduations (
            id, organization_id, client_id, modality_id, current_level_id, granted_at,
            granted_by_teacher_id, notes, created_at
        )
        select @Id, c.organization_id, c.id, gs.modality_id, level.id, @GrantedAt,
               @GrantedByTeacherId, @Notes, @CreatedAt
        from clients c
        join graduation_systems gs on gs.organization_id = c.organization_id and gs.modality_id = @ModalityId and gs.is_active = true
        join graduation_levels level on level.graduation_system_id = gs.id and level.id = @CurrentLevelId
        where c.id = @ClientId and c.organization_id = @OrganizationId and c.current_status = 'active'
        on conflict (organization_id, client_id, modality_id) do nothing
        returning id, organization_id as OrganizationId, client_id as ClientId, modality_id as ModalityId,
                  current_level_id as CurrentLevelId, granted_at as GrantedAt,
                  granted_by_teacher_id as GrantedByTeacherId, notes, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string LockGraduationForPromotion = @"
        select cg.current_level_id as CurrentLevelId, cg.modality_id as ModalityId,
               current_level.graduation_system_id as GraduationSystemId,
               current_level.hierarchy_order as CurrentOrder,
               next_level.hierarchy_order as NextOrder,
               next_level.requires_manual_evaluation as RequiresManualEvaluation
        from client_graduations cg
        join graduation_levels current_level on current_level.id = cg.current_level_id
        join graduation_levels next_level on next_level.id = @ToLevelId
                                              and next_level.graduation_system_id = current_level.graduation_system_id
        where cg.organization_id = @OrganizationId and cg.client_id = @ClientId and cg.modality_id = @ModalityId
        for update of cg;
        ";

    public const string InsertGraduationEvent = @"
        insert into graduation_events (
            id, organization_id, client_id, modality_id, from_level_id, to_level_id,
            event_type, event_date, teacher_id, justification, created_at
        )
        values (
            @Id, @OrganizationId, @ClientId, @ModalityId, @FromLevelId, @ToLevelId,
            'promotion', @EventDate, @TeacherId, @Justification, @CreatedAt
        )
        returning id, organization_id as OrganizationId, client_id as ClientId, modality_id as ModalityId,
                  from_level_id as FromLevelId, to_level_id as ToLevelId, event_type::text as EventType,
                  event_date as EventDate, teacher_id as TeacherId, justification as Justification, created_at as CreatedAt;
        ";

    public const string UpdateClientGraduation = @"
        update client_graduations
        set current_level_id = @ToLevelId, granted_at = @EventDate,
            granted_by_teacher_id = @TeacherId, notes = @Justification, updated_at = now()
        where organization_id = @OrganizationId and client_id = @ClientId and modality_id = @ModalityId;
        ";

    public const string ListGraduationEvents = @"
        select id, organization_id as OrganizationId, client_id as ClientId, modality_id as ModalityId,
               from_level_id as FromLevelId, to_level_id as ToLevelId, event_type::text as EventType,
               event_date as EventDate, teacher_id as TeacherId, justification as Justification, created_at as CreatedAt
        from graduation_events
        where organization_id = @OrganizationId and client_id = @ClientId and modality_id = @ModalityId
        order by event_date desc, created_at desc;
        ";

    public const string ListEligibleClients = @"
        with target_system as (
            select id, organization_id, modality_id
            from graduation_systems
            where id = @SystemId and organization_id = @OrganizationId and is_active = true
        ), attendance_stats as (
            select c.id as client_id,
                   count(a.id)::integer as attendance_count,
                   count(a.id) filter (where a.status in ('present', 'late'))::integer as attended_count,
                   count(distinct l.starts_at::date)::integer as training_days
            from clients c
            join enrollments e on e.client_id = c.id and e.organization_id = c.organization_id and e.status = 'active'
            join class_groups cg on cg.id = e.class_group_id and cg.organization_id = c.organization_id
            join target_system target on target.modality_id = cg.modality_id and target.organization_id = c.organization_id
            join lessons l on l.class_group_id = cg.id and l.organization_id = c.organization_id and l.status = 'completed'
            left join attendances a on a.lesson_id = l.id and a.client_id = c.id and a.organization_id = c.organization_id
            where c.organization_id = @OrganizationId and c.current_status = 'active'
            group by c.id
        )
        select c.id as ClientId, p.full_name as FullName,
               current_level.id as CurrentLevelId, next_level.id as NextLevelId,
               coalesce(stats.attended_count, 0) as AttendanceCount,
               case when coalesce(stats.attendance_count, 0) = 0 then 0
                    else 100.0 * stats.attended_count / stats.attendance_count end as AttendancePercentage,
               coalesce(stats.training_days, 0) as TrainingDays
        from client_graduations cg
        join clients c on c.id = cg.client_id and c.organization_id = cg.organization_id
        join persons p on p.id = c.person_id and p.organization_id = c.organization_id and p.deleted_at is null
        join graduation_levels current_level on current_level.id = cg.current_level_id
        join graduation_systems gs on gs.id = current_level.graduation_system_id
        join lateral (
            select level.* from graduation_levels level
            where level.graduation_system_id = gs.id and level.hierarchy_order > current_level.hierarchy_order
            order by level.hierarchy_order limit 1
        ) next_level on true
        left join attendance_stats stats on stats.client_id = c.id
        where cg.organization_id = @OrganizationId and gs.id = @SystemId
          and coalesce(stats.training_days, 0) >= next_level.min_training_days
          and coalesce(stats.attended_count, 0) >= next_level.min_lessons
          and (next_level.min_attendance_percentage is null
               or case when coalesce(stats.attendance_count, 0) = 0 then 0
                       else 100.0 * stats.attended_count / stats.attendance_count end >= next_level.min_attendance_percentage)
          and (next_level.min_age is null or p.birth_date <= (current_date - make_interval(years => next_level.min_age))::date)
          and next_level.requires_manual_evaluation = false
        order by p.full_name;
        ";

    public const string CreateFitnessGoal = @"
        insert into fitness_goals (
            id, organization_id, client_id, modality_id, title, description, goal_type,
            target_value, unit, starts_at, due_date, status, created_at
        )
        select @Id, c.organization_id, c.id, @ModalityId, @Title, @Description, @GoalType::goal_type,
               @TargetValue, @Unit, @StartsAt, @DueDate, 'active', @CreatedAt
        from clients c
        where c.id = @ClientId and c.organization_id = @OrganizationId and c.current_status = 'active'
          and (@ModalityId is null or exists (
              select 1 from modalities m where m.id = @ModalityId and m.organization_id = @OrganizationId and m.is_active = true
          ))
        returning id, organization_id as OrganizationId, client_id as ClientId, modality_id as ModalityId,
                  title, description, goal_type::text as GoalType, target_value as TargetValue,
                  current_value as CurrentValue, unit, starts_at as StartsAt, due_date as DueDate,
                  status::text as Status, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string ListFitnessGoals = @"
        select id, organization_id as OrganizationId, client_id as ClientId, modality_id as ModalityId,
               title, description, goal_type::text as GoalType, target_value as TargetValue,
               current_value as CurrentValue, unit, starts_at as StartsAt, due_date as DueDate,
               status::text as Status, created_at as CreatedAt, updated_at as UpdatedAt
        from fitness_goals
        where organization_id = @OrganizationId and (@ClientId is null or client_id = @ClientId)
        order by due_date, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string RecordFitnessProgress = @"
        with target as (
            select id, organization_id, target_value
            from fitness_goals
            where id = @FitnessGoalId and organization_id = @OrganizationId and status = 'active'
            for update
        ), inserted as (
            insert into fitness_progress_records (id, fitness_goal_id, recorded_value, notes, recorded_at, created_by_teacher_id)
            select @Id, target.id, @RecordedValue, @Notes, @RecordedAt, @CreatedByTeacherId from target
            returning id, fitness_goal_id as FitnessGoalId, recorded_value as RecordedValue,
                      notes, recorded_at as RecordedAt, created_by_teacher_id as CreatedByTeacherId
        ), updated as (
            update fitness_goals goal
            set current_value = @RecordedValue,
                status = case when @RecordedValue >= target.target_value then 'achieved'::goal_status else 'active'::goal_status end,
                updated_at = now()
            from target where goal.id = target.id
        )
        select * from inserted;
        ";

    public const string CancelFitnessGoal = @"
        update fitness_goals
        set status = 'canceled', updated_at = now()
        where organization_id = @OrganizationId and id = @GoalId and status = 'active'
        returning id;
        ";
}