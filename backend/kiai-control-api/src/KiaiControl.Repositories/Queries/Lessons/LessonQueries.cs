namespace KiaiControl.Repositories.Queries.Lessons;

public static class LessonQueries
{
    public const string Insert = @"
        insert into lessons (id, organization_id, class_group_id, starts_at, ends_at, status, created_at)
        select @Id, cg.organization_id, cg.id, @StartsAt, @EndsAt, 'scheduled', @CreatedAt
        from class_groups cg
        where cg.id = @ClassGroupId
          and cg.organization_id = @OrganizationId
          and cg.status = 'active'
        returning
            id,
            organization_id as OrganizationId,
            class_group_id as ClassGroupId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            class_group_id as ClassGroupId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from lessons
        where organization_id = @OrganizationId
        order by starts_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            class_group_id as ClassGroupId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from lessons
        where organization_id = @OrganizationId and id = @Id;
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
            class_group_id as ClassGroupId,
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
            class_group_id as ClassGroupId,
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
            class_group_id as ClassGroupId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string GenerateFromSchedules = @"
        with target as (
            select cg.id, cg.organization_id
            from class_groups cg
            where cg.id = @ClassGroupId
              and cg.organization_id = @OrganizationId
              and cg.status = 'active'
        ), scheduled_lessons as (
            select
                target.organization_id,
                target.id as class_group_id,
                ((days.lesson_day + schedule.starts_at) at time zone coalesce(settings.timezone, 'UTC')) as starts_at,
                ((days.lesson_day + schedule.ends_at) at time zone coalesce(settings.timezone, 'UTC')) as ends_at
            from target
            join class_schedules schedule on schedule.class_group_id = target.id
            cross join lateral generate_series(
                @StartDate::date,
                @EndDate::date,
                interval '1 day'
            ) as days(lesson_day)
            left join organization_settings settings on settings.organization_id = target.organization_id
            where extract(dow from days.lesson_day)::integer = schedule.day_of_week
        )
        insert into lessons (id, organization_id, class_group_id, starts_at, ends_at, status, created_at)
        select gen_random_uuid(), organization_id, class_group_id, starts_at, ends_at, 'scheduled', now()
        from scheduled_lessons
        on conflict (organization_id, class_group_id, starts_at) do nothing
        returning
            id,
            organization_id as OrganizationId,
            class_group_id as ClassGroupId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            status::text as Status,
            cancellation_reason as CancellationReason,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";
}