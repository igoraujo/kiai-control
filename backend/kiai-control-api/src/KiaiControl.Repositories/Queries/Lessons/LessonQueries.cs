namespace KiaiControl.Repositories.Queries.Lessons;

public static class LessonQueries
{
    public const string Insert = @"
        insert into lessons (id, organization_id, modality_id, starts_at, ends_at, status, created_at)
        select @Id, modality.organization_id, modality.id, @StartsAt, @EndsAt, 'scheduled', @CreatedAt
        from modalities modality
        where modality.id = @ModalityId
          and modality.organization_id = @OrganizationId
          and modality.is_active = true
        returning
            id,
            organization_id as OrganizationId,
            modality_id as ModalityId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            lessons.id,
            lessons.organization_id as OrganizationId,
            lessons.modality_id as ModalityId,
            lessons.modality_schedule_id as ModalityScheduleId,
            lessons.starts_at as StartsAt,
            lessons.ends_at as EndsAt,
            lessons.status::text as Status,
            lessons.cancellation_reason as CancellationReason,
            lessons.created_at as CreatedAt,
            lessons.updated_at as UpdatedAt,
            modality.name as ModalityName,
            branch.name as BranchName
        from lessons
        left join modalities modality on modality.id = lessons.modality_id and modality.organization_id = lessons.organization_id
        left join modality_schedules modality_schedule on modality_schedule.id = lessons.modality_schedule_id
            and modality_schedule.organization_id = lessons.organization_id
        left join branches branch on branch.id = modality_schedule.branch_id and branch.organization_id = lessons.organization_id
        where lessons.organization_id = @OrganizationId
        order by lessons.starts_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            lessons.id,
            lessons.organization_id as OrganizationId,
            lessons.modality_id as ModalityId,
            lessons.modality_schedule_id as ModalityScheduleId,
            lessons.starts_at as StartsAt,
            lessons.ends_at as EndsAt,
            lessons.status::text as Status,
            lessons.cancellation_reason as CancellationReason,
            lessons.created_at as CreatedAt,
            lessons.updated_at as UpdatedAt,
            modality.name as ModalityName,
            branch.name as BranchName
        from lessons
        left join modalities modality on modality.id = lessons.modality_id and modality.organization_id = lessons.organization_id
        left join modality_schedules modality_schedule on modality_schedule.id = lessons.modality_schedule_id
            and modality_schedule.organization_id = lessons.organization_id
        left join branches branch on branch.id = modality_schedule.branch_id and branch.organization_id = lessons.organization_id
        where lessons.organization_id = @OrganizationId and lessons.id = @Id;
        ";

    public const string Update = @"
        update lessons
        set starts_at = @StartsAt, ends_at = @EndsAt, updated_at = @UpdatedAt
        where organization_id = @OrganizationId
          and id = @Id
          and status in ('scheduled', 'rescheduled')
        returning
            id,
            organization_id as OrganizationId,
            modality_id as ModalityId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string Cancel = @"
        update lessons
        set status = 'canceled', cancellation_reason = @Reason, updated_at = @UpdatedAt
        where organization_id = @OrganizationId
          and id = @Id
          and status in ('scheduled', 'rescheduled')
        returning
            id,
            organization_id as OrganizationId,
            modality_id as ModalityId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string Reschedule = @"
        update lessons
        set starts_at = @StartsAt, ends_at = @EndsAt, status = 'rescheduled', updated_at = @UpdatedAt
        where organization_id = @OrganizationId
          and id = @Id
          and status in ('scheduled', 'rescheduled')
        returning
            id,
            organization_id as OrganizationId,
            modality_id as ModalityId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string GenerateFromModalitySchedules = @"
        with target as (
            select modality.id, modality.organization_id
            from modalities modality
            where modality.id = @ModalityId
              and modality.organization_id = @OrganizationId
              and modality.is_active = true
        ), scheduled_lessons as (
            select
                target.organization_id,
                target.id as modality_id,
                schedule.id as modality_schedule_id,
                ((days.lesson_day + schedule.starts_at) at time zone coalesce(settings.timezone, 'UTC')) as starts_at,
                ((days.lesson_day + schedule.ends_at) at time zone coalesce(settings.timezone, 'UTC')) as ends_at
            from target
            join modality_schedules schedule on schedule.modality_id = target.id
                and schedule.organization_id = target.organization_id
                and schedule.is_active = true
            cross join lateral generate_series(
                @StartDate::date,
                @EndDate::date,
                interval '1 day'
            ) as days(lesson_day)
            left join organization_settings settings on settings.organization_id = target.organization_id
            where extract(dow from days.lesson_day)::integer = schedule.day_of_week
        ), inserted_lessons as (
            insert into lessons (
                id, organization_id, modality_id, modality_schedule_id,
                starts_at, ends_at, status, created_at
            )
            select gen_random_uuid(), organization_id, modality_id, modality_schedule_id,
                starts_at, ends_at, 'scheduled', now()
            from scheduled_lessons
            on conflict (organization_id, modality_schedule_id, starts_at) do nothing
            returning id, organization_id, modality_id, modality_schedule_id,
                starts_at, ends_at, status, cancellation_reason, created_at, updated_at
        ), inserted_instructors as (
            insert into lesson_instructors (id, lesson_id, teacher_id, is_substitute, created_at)
            select gen_random_uuid(), inserted.id, schedule.teacher_id, false, now()
            from inserted_lessons inserted
            join modality_schedules schedule on schedule.id = inserted.modality_schedule_id
                and schedule.organization_id = inserted.organization_id
            where schedule.teacher_id is not null
            on conflict (lesson_id, teacher_id) do nothing
            returning lesson_id
        )
        select inserted.id, inserted.organization_id as OrganizationId,
            inserted.modality_id as ModalityId, inserted.modality_schedule_id as ModalityScheduleId,
            inserted.starts_at as StartsAt, inserted.ends_at as EndsAt,
            inserted.status::text as Status, inserted.cancellation_reason as CancellationReason,
            inserted.created_at as CreatedAt, inserted.updated_at as UpdatedAt,
            modality.name as ModalityName, branch.name as BranchName
        from inserted_lessons inserted
        join modalities modality on modality.id = inserted.modality_id
            and modality.organization_id = inserted.organization_id
        join modality_schedules schedule on schedule.id = inserted.modality_schedule_id
            and schedule.organization_id = inserted.organization_id
        left join branches branch on branch.id = schedule.branch_id
            and branch.organization_id = inserted.organization_id;
        ";
}