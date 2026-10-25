namespace KiaiControl.Repositories.Queries.Attendance;

public static class AttendanceQueries
{
    public const string Insert = @"
        insert into attendances (id, organization_id, lesson_id, client_id, status, checked_in_at, created_at)
        values (@Id, @OrganizationId, @LessonId, @ClientId, @Status::attendance_status, @AttendanceDate, @CreatedAt)
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId, checked_in_at as AttendanceDate, status, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            lesson_id as LessonId,
            client_id as ClientId,
            checked_in_at as AttendanceDate,
            status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from attendances
        where organization_id = @OrganizationId
        order by created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            lesson_id as LessonId,
            client_id as ClientId,
            checked_in_at as AttendanceDate,
            status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from attendances
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string Update = @"
        update attendances
        set
            lesson_id = @LessonId,
            client_id = @ClientId,
            checked_in_at = @AttendanceDate,
            status = @Status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId, checked_in_at as AttendanceDate, status, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string Delete = @"
        delete from attendances
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string ListByLesson = @"
        select
            id,
            organization_id as OrganizationId,
            lesson_id as LessonId,
            client_id as ClientId,
            checked_in_at as AttendanceDate,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from attendances
        where organization_id = @OrganizationId and lesson_id = @LessonId
        order by created_at, client_id;
        ";

    public const string LockLessonForCall = @"
        select l.class_group_id
        from lessons l
        where l.organization_id = @OrganizationId
          and l.id = @LessonId
          and l.status in ('scheduled', 'completed')
        for update;
        ";

    public const string UpsertCallEntry = @"
        insert into attendances (
            id, organization_id, lesson_id, client_id, status, checked_in_at, created_at
        )
        select
            @Id, @OrganizationId, l.id, e.client_id, @Status::attendance_status, @AttendanceDate, @CreatedAt
        from lessons l
        join enrollments e
          on e.organization_id = l.organization_id
         and e.class_group_id = l.class_group_id
         and e.client_id = @ClientId
         and e.status = 'active'
        join clients c
          on c.id = e.client_id
         and c.organization_id = l.organization_id
         and c.current_status = 'active'
        where l.organization_id = @OrganizationId
          and l.id = @LessonId
          and l.status in ('scheduled', 'completed')
        on conflict (organization_id, lesson_id, client_id)
        do update set
            status = excluded.status,
            checked_in_at = excluded.checked_in_at,
            updated_at = now()
        returning
            id,
            organization_id as OrganizationId,
            lesson_id as LessonId,
            client_id as ClientId,
            checked_in_at as AttendanceDate,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";
}
