namespace KiaiControl.Repositories.Queries.Modalities;

public static class ModalityScheduleQueries
{
    public const string Insert = @"
        with inserted as (
            insert into modality_schedules (
                id, organization_id, modality_id, branch_id, teacher_id, day_of_week,
                starts_at, ends_at, is_active, created_at
            )
            select
                @Id, modality.organization_id, modality.id, branch.id, teacher.id,
                @DayOfWeek, @StartsAt, @EndsAt, true, @CreatedAt
            from modalities modality
            left join branches branch on branch.id = @BranchId
                and branch.organization_id = modality.organization_id
            left join teachers teacher on teacher.id = @TeacherId
                and teacher.organization_id = modality.organization_id
                and teacher.current_status = 'active'
            where modality.organization_id = @OrganizationId
              and modality.id = @ModalityId
              and modality.is_active = true
              and (@BranchId is null or branch.id is not null)
              and (@TeacherId is null or teacher.id is not null)
            on conflict (organization_id, modality_id, branch_id, day_of_week, starts_at) do nothing
            returning id, organization_id, modality_id, branch_id, teacher_id,
                day_of_week, starts_at, ends_at, is_active, created_at, updated_at
        )
        select inserted.id, inserted.organization_id as OrganizationId,
            inserted.modality_id as ModalityId, inserted.branch_id as BranchId,
            inserted.teacher_id as TeacherId, branch.name as BranchName,
            coalesce(teacher_person.full_name, teacher_person.preferred_name) as TeacherName,
            inserted.day_of_week as DayOfWeek, inserted.starts_at as StartsAt,
            inserted.ends_at as EndsAt, inserted.is_active as IsActive,
            inserted.created_at as CreatedAt, inserted.updated_at as UpdatedAt
        from inserted
        left join branches branch on branch.id = inserted.branch_id
            and branch.organization_id = inserted.organization_id
        left join teachers teacher on teacher.id = inserted.teacher_id
            and teacher.organization_id = inserted.organization_id
        left join persons teacher_person on teacher_person.id = teacher.person_id
            and teacher_person.deleted_at is null;";

    public const string List = @"
        select schedule.id, schedule.organization_id as OrganizationId,
            schedule.modality_id as ModalityId, schedule.branch_id as BranchId,
            schedule.teacher_id as TeacherId, branch.name as BranchName,
            coalesce(teacher_person.full_name, teacher_person.preferred_name) as TeacherName,
            schedule.day_of_week as DayOfWeek, schedule.starts_at as StartsAt,
            schedule.ends_at as EndsAt, schedule.is_active as IsActive,
            schedule.created_at as CreatedAt, schedule.updated_at as UpdatedAt
        from modality_schedules schedule
        left join branches branch on branch.id = schedule.branch_id
            and branch.organization_id = schedule.organization_id
        left join teachers teacher on teacher.id = schedule.teacher_id
            and teacher.organization_id = schedule.organization_id
        left join persons teacher_person on teacher_person.id = teacher.person_id
            and teacher_person.deleted_at is null
        where schedule.organization_id = @OrganizationId
          and schedule.modality_id = @ModalityId
          and schedule.is_active = true
        order by schedule.day_of_week, schedule.starts_at;";

    public const string Deactivate = @"
        update modality_schedules
        set is_active = false, updated_at = @UpdatedAt
        where organization_id = @OrganizationId
          and modality_id = @ModalityId
          and id = @ScheduleId
          and is_active = true;";

    public const string GetModality = @"
        select true
        from modalities
        where organization_id = @OrganizationId
          and id = @ModalityId
          and is_active = true;";
}
