namespace KiaiControl.Repositories.Queries.Progression;

public static class ProgressionQueries
{
    public const string CreateGraduationSystem = @"
        insert into graduation_systems (id, organization_id, modality_id, name, description, is_active, created_at)
        select @Id, m.organization_id, m.id, @Name, @Description, true, @CreatedAt
        from modalities m
        where m.id = @ModalityId and m.organization_id = @OrganizationId
          and m.is_active = true and m.type = 'martial_art'
        returning id, organization_id as OrganizationId, modality_id as ModalityId, name, description, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string ListGraduationSystems = @"
        select id, organization_id as OrganizationId, modality_id as ModalityId, name, description, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
        from graduation_systems
        where organization_id = @OrganizationId
          and exists (
              select 1 from modalities m
              where m.id = graduation_systems.modality_id and m.type = 'martial_art' and m.is_active = true
          )
        order by name;
        ";

    public const string ListGraduationBelts = @"
        select belt.id, belt.graduation_system_id as GraduationSystemId, belt.name,
               belt.hierarchy_order as HierarchyOrder, belt.degree_count as DegreeCount,
               level.min_lessons as MinLessons, level.min_training_days as MinTrainingDays,
               level.min_attendance_percentage as MinAttendancePercentage,
               level.requires_manual_evaluation as RequiresManualEvaluation
        from graduation_belts belt
        join graduation_systems system on system.id = belt.graduation_system_id
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        join graduation_levels level on level.belt_id = belt.id and level.degree_number = 0
        where system.organization_id = @OrganizationId and system.id = @SystemId
        order by belt.hierarchy_order;
        ";

    public const string LockGraduationSystem = @"
        select system.id,
               (select coalesce(max(belt.hierarchy_order), 0)
                from graduation_belts belt where belt.graduation_system_id = system.id) as CurrentOrder,
               exists (
                   select 1 from graduation_belts belt
                   where belt.graduation_system_id = system.id and lower(belt.name) = lower(@Name)
               ) as NameExists
        from graduation_systems system
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        where system.organization_id = @OrganizationId and system.id = @SystemId and system.is_active = true
        for update of system;
        ";

    public const string InsertGraduationBelt = @"
        insert into graduation_belts (id, graduation_system_id, name, hierarchy_order, degree_count)
        values (@Id, @SystemId, @Name, @HierarchyOrder, @DegreeCount);
        insert into graduation_levels (
            id, graduation_system_id, belt_id, degree_number, name, hierarchy_order,
            min_training_days, min_attendance_percentage, min_lessons, requires_manual_evaluation
        )
        values (
            @LevelId, @SystemId, @Id, 0, @Name, @LevelOrder,
            @MinTrainingDays, @MinAttendancePercentage, @MinLessons, @RequiresManualEvaluation
        );
        ";

    public const string GetGraduationBelt = @"
        select belt.id, belt.graduation_system_id as GraduationSystemId, belt.name,
               belt.hierarchy_order as HierarchyOrder, belt.degree_count as DegreeCount,
               level.min_lessons as MinLessons, level.min_training_days as MinTrainingDays,
               level.min_attendance_percentage as MinAttendancePercentage,
               level.requires_manual_evaluation as RequiresManualEvaluation
        from graduation_belts belt
        join graduation_levels level on level.belt_id = belt.id and level.degree_number = 0
        where belt.id = @BeltId;
        ";

    public const string GraduationBeltNameExists = @"
        select exists (
            select 1 from graduation_belts
            where graduation_system_id = @SystemId
              and id <> @BeltId
              and lower(name) = lower(@Name)
        );
        ";

    public const string GraduationBeltLevelNameExists = @"
        select exists (
            select 1
            from graduation_levels existing
            where existing.graduation_system_id = @SystemId
              and existing.belt_id <> @BeltId
              and (
                  lower(existing.name) = lower(@Name)
                  or exists (
                      select 1
                      from graduation_levels degree
                      where degree.belt_id = @BeltId
                        and degree.degree_number > 0
                        and lower(existing.name) = lower(@Name || ' - ' || degree.degree_number::text || 'º grau')
                  )
              )
        );
        ";

    public const string UpdateGraduationBelt = @"
        update graduation_belts
        set name = @Name, degree_count = @DegreeCount
        where id = @BeltId and graduation_system_id = @SystemId;

        update graduation_levels
        set name = case
                       when degree_number = 0 then @Name
                       else @Name || ' - ' || degree_number::text || 'º grau'
                   end,
            min_lessons = case when degree_number = 0 then @MinLessons else min_lessons end,
            min_training_days = case when degree_number = 0 then @MinTrainingDays else min_training_days end,
            min_attendance_percentage = case
                when degree_number = 0 then @MinAttendancePercentage
                else min_attendance_percentage
            end,
            requires_manual_evaluation = case
                when degree_number = 0 then @RequiresManualEvaluation
                else requires_manual_evaluation
            end
        where belt_id = @BeltId;
        ";

    public const string GraduationBeltHasHistory = @"
        select exists (
            select 1
            from graduation_levels level
            where level.belt_id = @BeltId
              and (
                  exists (select 1 from client_graduations graduation where graduation.current_level_id = level.id)
                  or exists (
                      select 1 from graduation_events event
                      where event.from_level_id = level.id or event.to_level_id = level.id
                  )
              )
        );
        ";

    public const string DeleteGraduationBeltLevels = @"
        delete from graduation_levels
        where belt_id = @BeltId;
        ";

    public const string DeleteGraduationBelt = @"
        delete from graduation_belts
        where id = @BeltId and graduation_system_id = @SystemId
        returning id;
        ";

    public const string LockGraduationBelt = @"
        select belt.id, belt.name, belt.hierarchy_order as HierarchyOrder,
               belt.degree_count as DegreeCount,
               (select coalesce(max(level.degree_number), 0)
                from graduation_levels level where level.belt_id = belt.id) as CurrentDegree
        from graduation_belts belt
        join graduation_systems system on system.id = belt.graduation_system_id
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        join graduation_levels base_level on base_level.belt_id = belt.id and base_level.degree_number = 0
        where system.organization_id = @OrganizationId and system.id = @SystemId and belt.id = @BeltId
        for update of belt;
        ";

    public const string LockGraduationDegree = @"
        select level.degree_number as DegreeNumber,
               (select coalesce(max(other.degree_number), 0)
                from graduation_levels other where other.belt_id = level.belt_id) as HighestDegree
        from graduation_levels level
        join graduation_systems system on system.id = level.graduation_system_id
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        where system.organization_id = @OrganizationId and system.id = @SystemId
          and level.belt_id = @BeltId and level.id = @DegreeId and level.degree_number > 0
        for update of level;
        ";

    public const string GraduationDegreeHasHistory = @"
        select exists (
            select 1 from client_graduations where current_level_id = @DegreeId
        ) or exists (
            select 1 from graduation_events
            where from_level_id = @DegreeId or to_level_id = @DegreeId
        );
        ";

    public const string UpdateGraduationDegree = @"
        update graduation_levels
        set min_lessons = @MinLessons,
            min_training_days = @MinTrainingDays,
            min_attendance_percentage = @MinAttendancePercentage,
            min_age = @MinAge,
            requires_manual_evaluation = @RequiresManualEvaluation
        where id = @DegreeId and belt_id = @BeltId and degree_number > 0;
        ";

    public const string DeleteGraduationDegree = @"
        delete from graduation_levels
        where id = @DegreeId and belt_id = @BeltId and degree_number > 0
        returning id;
        ";

    public const string InsertGraduationDegree = @"
        insert into graduation_levels (
            id, graduation_system_id, belt_id, degree_number, name, hierarchy_order,
            min_training_days, min_attendance_percentage, min_lessons, min_age,
            requires_manual_evaluation, created_at
        )
        select @Id, @SystemId, belt.id, @DegreeNumber,
               belt.name || ' - ' || @DegreeNumber::text || 'º grau',
               belt.hierarchy_order * 100 + @DegreeNumber,
               @MinTrainingDays, @MinAttendancePercentage, @MinLessons, @MinAge,
               @RequiresManualEvaluation, @CreatedAt
        from graduation_belts belt
        join graduation_systems system on system.id = belt.graduation_system_id
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        where system.organization_id = @OrganizationId and system.id = @SystemId and belt.id = @BeltId
        returning id, graduation_system_id as GraduationSystemId, belt_id as BeltId,
                  (select name from graduation_belts where id = belt_id) as BeltName,
                  degree_number as DegreeNumber, name, hierarchy_order as HierarchyOrder,
                  min_training_days as MinTrainingDays,
                  min_attendance_percentage as MinAttendancePercentage,
                  min_lessons as MinLessons, min_age as MinAge,
                  requires_manual_evaluation as RequiresManualEvaluation, created_at as CreatedAt;
        ";

    public const string ListGraduationLevels = @"
        select l.id, l.graduation_system_id as GraduationSystemId, l.belt_id as BeltId,
               belt.name as BeltName, l.degree_number as DegreeNumber,
               l.name, l.hierarchy_order as HierarchyOrder,
               l.min_training_days as MinTrainingDays, l.min_attendance_percentage as MinAttendancePercentage,
               l.min_lessons as MinLessons, l.min_age as MinAge, l.requires_manual_evaluation as RequiresManualEvaluation,
               l.created_at as CreatedAt
        from graduation_levels l
        join graduation_systems gs on gs.id = l.graduation_system_id
        join modalities modality on modality.id = gs.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        join graduation_belts belt on belt.id = l.belt_id
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
        join modalities modality on modality.id = gs.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        join graduation_levels level on level.graduation_system_id = gs.id and level.id = @CurrentLevelId
        where c.id = @ClientId and c.organization_id = @OrganizationId and c.current_status = 'active'
          and exists (
              select 1
              from person_modalities assignment
              where assignment.person_id = c.person_id
                and assignment.modality_id = gs.modality_id
          )
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
               next_level.requires_manual_evaluation as RequiresManualEvaluation,
               (
                   coalesce(stats.training_days, 0) >= next_level.min_training_days
                   and coalesce(stats.attended_count, 0) >= next_level.min_lessons
                   and (
                       next_level.min_attendance_percentage is null
                       or case when coalesce(stats.attendance_count, 0) = 0 then 0
                               else 100.0 * stats.attended_count / stats.attendance_count end
                           >= next_level.min_attendance_percentage
                   )
                   and (
                       next_level.min_age is null
                       or p.birth_date <= (current_date - make_interval(years => next_level.min_age))::date
                   )
               ) as IsEligible
        from client_graduations cg
        join clients c on c.id = cg.client_id and c.organization_id = cg.organization_id
        join persons p on p.id = c.person_id and p.organization_id = c.organization_id and p.deleted_at is null
        join graduation_levels current_level on current_level.id = cg.current_level_id
        join graduation_levels next_level on next_level.id = @ToLevelId
                                              and next_level.graduation_system_id = current_level.graduation_system_id
        join graduation_systems system on system.id = current_level.graduation_system_id
        join modalities modality on modality.id = system.modality_id
            and modality.type = 'martial_art' and modality.is_active = true
        left join lateral (
            select count(a.id)::integer as attendance_count,
                   count(a.id) filter (where a.status in ('present', 'late'))::integer as attended_count,
                   count(distinct lesson.starts_at::date)::integer as training_days
            from lessons lesson
            left join attendances a on a.lesson_id = lesson.id
                and a.client_id = cg.client_id
                and a.organization_id = cg.organization_id
            where lesson.organization_id = cg.organization_id
              and lesson.modality_id = cg.modality_id
              and lesson.status = 'completed'
              and lesson.starts_at::date > cg.granted_at
        ) stats on true
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
          and exists (
              select 1 from modalities modality
              where modality.id = @ModalityId and modality.organization_id = @OrganizationId
                and modality.type = 'martial_art'
          )
        order by event_date desc, created_at desc;
        ";

    public const string ListEligibleClients = @"
        with target_system as (
            select system.id, system.organization_id, system.modality_id
            from graduation_systems system
            join modalities modality on modality.id = system.modality_id
                and modality.type = 'martial_art' and modality.is_active = true
            where system.id = @SystemId and system.organization_id = @OrganizationId and system.is_active = true
        ), eligible_lessons as (
            select distinct c.id as client_id, lesson.id as lesson_id, lesson.starts_at
            from clients c
            join target_system target on target.organization_id = c.organization_id
            join client_graduations graduation on graduation.organization_id = c.organization_id
                and graduation.client_id = c.id and graduation.modality_id = target.modality_id
            join person_modalities assignment on assignment.person_id = c.person_id
                and assignment.modality_id = target.modality_id
            join modalities modality on modality.id = assignment.modality_id
                and modality.organization_id = c.organization_id
                and modality.is_active = true
            join lessons lesson on lesson.organization_id = c.organization_id
                and lesson.modality_id = target.modality_id
                and lesson.status = 'completed'
                and lesson.starts_at::date > graduation.granted_at
            where c.organization_id = @OrganizationId
              and c.current_status = 'active'
        ), attendance_stats as (
            select eligible.client_id,
                   count(a.id)::integer as attendance_count,
                   count(a.id) filter (where a.status in ('present', 'late'))::integer as attended_count,
                   count(distinct eligible.starts_at::date)::integer as training_days
            from eligible_lessons eligible
            left join attendances a on a.lesson_id = eligible.lesson_id
                and a.client_id = eligible.client_id
                and a.organization_id = @OrganizationId
            group by eligible.client_id
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

    public const string ListFitnessProgressRecords = @"
        select record.id, record.fitness_goal_id as FitnessGoalId,
               record.recorded_value as RecordedValue, record.notes,
               record.recorded_at as RecordedAt,
               record.created_by_teacher_id as CreatedByTeacherId
        from fitness_progress_records record
        join fitness_goals goal on goal.id = record.fitness_goal_id
        where goal.organization_id = @OrganizationId and goal.id = @GoalId
        order by record.recorded_at desc, record.id desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string CancelFitnessGoal = @"
        update fitness_goals
        set status = 'canceled', updated_at = now()
        where organization_id = @OrganizationId and id = @GoalId and status = 'active'
        returning id;
        ";
}