namespace KiaiControl.Repositories.Queries.ClassGroups;

public static class ClassGroupQueries
{
    public const string Insert = @"
        insert into class_groups (
            id, organization_id, branch_id, modality_id, name, description, capacity, status, created_at
        )
        select
            @Id, @OrganizationId, @BranchId, @ModalityId, @Name, @Description, @Capacity, 'active', @CreatedAt
        where exists (
            select 1 from modalities
            where id = @ModalityId and organization_id = @OrganizationId and is_active = true
        )
          and (@BranchId is null or exists (
              select 1 from branches where id = @BranchId and organization_id = @OrganizationId
          ))
        on conflict (organization_id, name) do nothing
        returning
            id,
            organization_id as OrganizationId,
            branch_id as BranchId,
            modality_id as ModalityId,
            name,
            description,
            capacity,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            branch_id as BranchId,
            modality_id as ModalityId,
            name,
            description,
            capacity,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from class_groups
        where organization_id = @OrganizationId
        order by name
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            branch_id as BranchId,
            modality_id as ModalityId,
            name,
            description,
            capacity,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from class_groups
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string Update = @"
        update class_groups
        set
            branch_id = @BranchId,
            modality_id = @ModalityId,
            name = @Name,
            description = @Description,
            capacity = @Capacity,
            status = @Status::class_group_status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId
          and id = @Id
          and exists (
              select 1 from modalities
              where id = @ModalityId and organization_id = @OrganizationId and is_active = true
          )
          and (@BranchId is null or exists (
              select 1 from branches where id = @BranchId and organization_id = @OrganizationId
          ))
        returning
            id,
            organization_id as OrganizationId,
            branch_id as BranchId,
            modality_id as ModalityId,
            name,
            description,
            capacity,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string Archive = @"
        update class_groups
        set status = 'archived', updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id and status <> 'archived';
        ";

        public const string CreateSchedule = @"
                insert into class_schedules (id, class_group_id, day_of_week, starts_at, ends_at, created_at)
                select @Id, cg.id, @DayOfWeek, @StartsAt::time, @EndsAt::time, @CreatedAt
                from class_groups cg
                where cg.id = @ClassGroupId
                    and cg.organization_id = @OrganizationId
                    and cg.status = 'active'
                on conflict (class_group_id, day_of_week, starts_at) do nothing
                returning
                        id,
                        class_group_id as ClassGroupId,
                        day_of_week as DayOfWeek,
                        starts_at as StartsAt,
                        ends_at as EndsAt,
                        created_at as CreatedAt;
                ";

        public const string DeleteSchedule = @"
                delete from class_schedules s
                using class_groups cg
                where s.id = @ScheduleId
                    and s.class_group_id = @ClassGroupId
                    and cg.id = s.class_group_id
                    and cg.organization_id = @OrganizationId;
                ";

        public const string ListSchedules = @"
                select
                    s.id,
                    s.class_group_id as ClassGroupId,
                    s.day_of_week as DayOfWeek,
                    s.starts_at as StartsAt,
                    s.ends_at as EndsAt,
                    s.created_at as CreatedAt
                from class_schedules s
                join class_groups cg on cg.id = s.class_group_id
                where cg.organization_id = @OrganizationId
                    and cg.id = @ClassGroupId
                order by s.day_of_week, s.starts_at;
                ";

        public const string LockEnrollmentTarget = @"
                select capacity
                from class_groups
                where id = @ClassGroupId
                    and organization_id = @OrganizationId
                    and status = 'active'
                for update;
                ";

        public const string CountOtherActiveEnrollments = @"
                select count(*)
                from enrollments
                where organization_id = @OrganizationId
                    and class_group_id = @ClassGroupId
                    and client_id <> @ClientId
                    and status = 'active';
                ";

        public const string CreateEnrollment = @"
                insert into enrollments (id, organization_id, class_group_id, client_id, enrolled_at, status)
                select @Id, @OrganizationId, cg.id, c.id, @EnrolledAt, 'active'
                from class_groups cg
                join clients c on c.id = @ClientId
                                         and c.organization_id = @OrganizationId
                                         and c.current_status = 'active'
                where cg.id = @ClassGroupId
                    and cg.organization_id = @OrganizationId
                    and cg.status = 'active'
                on conflict (organization_id, class_group_id, client_id)
                do update set status = 'active', ended_at = null, enrolled_at = excluded.enrolled_at
                returning
                        id,
                        organization_id as OrganizationId,
                        class_group_id as ClassGroupId,
                        client_id as ClientId,
                        enrolled_at as EnrolledAt,
                        ended_at as EndedAt,
                        status::text as Status;
                ";

        public const string CancelEnrollment = @"
                update enrollments e
                set status = 'canceled', ended_at = @EndedAt
                from class_groups cg
                where e.id = @EnrollmentId
                    and e.class_group_id = @ClassGroupId
                    and e.organization_id = @OrganizationId
                    and e.status = 'active'
                    and cg.id = e.class_group_id
                    and cg.organization_id = @OrganizationId;
                ";

        public const string ListEnrollments = @"
                select
                    e.id,
                    e.organization_id as OrganizationId,
                    e.class_group_id as ClassGroupId,
                    e.client_id as ClientId,
                    p.full_name as ClientName,
                    e.enrolled_at as EnrolledAt,
                    e.ended_at as EndedAt,
                    e.status::text as Status
                from enrollments e
                join class_groups cg on cg.id = e.class_group_id
                    and cg.organization_id = e.organization_id
                join clients c on c.id = e.client_id
                    and c.organization_id = e.organization_id
                join persons p on p.id = c.person_id
                where e.organization_id = @OrganizationId
                    and e.class_group_id = @ClassGroupId
                    and p.deleted_at is null
                order by e.status = 'active' desc, p.full_name;
                ";

        public const string AssignTeacher = @"
                insert into class_group_teachers (
                        id, class_group_id, teacher_id, is_primary, starts_at, ends_at, created_at
                )
                select @Id, cg.id, t.id, @IsPrimary, @StartsAt, @EndsAt, now()
                from class_groups cg
                join teachers t on t.id = @TeacherId
                                             and t.organization_id = @OrganizationId
                                             and t.current_status = 'active'
                where cg.id = @ClassGroupId
                    and cg.organization_id = @OrganizationId
                    and cg.status = 'active'
                on conflict (class_group_id, teacher_id, starts_at)
                do update set is_primary = excluded.is_primary, ends_at = excluded.ends_at;
                ";

        public const string RemoveTeacherAssignment = @"
                delete from class_group_teachers cgt
                using class_groups cg
                where cgt.class_group_id = cg.id
                    and cg.organization_id = @OrganizationId
                    and cg.id = @ClassGroupId
                    and cgt.teacher_id = @TeacherId
                    and cgt.starts_at = @StartsAt;
                ";

        public const string ListTeacherAssignments = @"
                select
                    cgt.id,
                    cgt.class_group_id as ClassGroupId,
                    cgt.teacher_id as TeacherId,
                    p.full_name as TeacherName,
                    cgt.is_primary as IsPrimary,
                    cgt.starts_at as StartsAt,
                    cgt.ends_at as EndsAt
                from class_group_teachers cgt
                join class_groups cg on cg.id = cgt.class_group_id
                join teachers t on t.id = cgt.teacher_id
                    and t.organization_id = cg.organization_id
                join persons p on p.id = t.person_id
                where cg.organization_id = @OrganizationId
                    and cg.id = @ClassGroupId
                    and p.deleted_at is null
                order by cgt.is_primary desc, cgt.starts_at desc, p.full_name;
                ";
}