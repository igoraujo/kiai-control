namespace KiaiControl.Repositories.Queries.Attendance;

public static class AttendanceQueries
{
    public const string Insert = @"
        insert into attendances (
            id, organization_id, lesson_id, client_id, status, checked_in_at,
            validated_at, validated_by_person_id, created_at
        )
        values (
            @Id, @OrganizationId, @LessonId, @ClientId, @Status::attendance_status, @AttendanceDate,
            @ValidatedAt, @ValidatedByPersonId, @CreatedAt
        )
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId,
            checked_in_at as AttendanceDate, status::text as Status, validated_at as ValidatedAt,
            validated_by_person_id as ValidatedByPersonId, created_at as CreatedAt, updated_at as UpdatedAt;";

    public const string CreateCheckIn = @"
        insert into attendances (
            id, organization_id, lesson_id, client_id, status, checked_in_at, created_at
        )
        select
            @Id, l.organization_id, l.id, c.id, 'pending_confirmation'::attendance_status, @CheckedInAt, @CreatedAt
        from lessons l
        join clients c on c.organization_id = l.organization_id
            and c.current_status = 'active'
        join persons p on p.id = c.person_id
            and p.deleted_at is null
            and p.id = @PersonId
        where l.organization_id = @OrganizationId
            and l.id = @LessonId
            and exists (
                select 1 from person_modalities pm
                join modalities modality on modality.id = pm.modality_id
                    and modality.organization_id = l.organization_id
                    and modality.is_active = true
                where pm.person_id = c.person_id
                  and pm.modality_id = l.modality_id
            )
            and l.status = 'scheduled'
            and l.starts_at <= @CheckedInAt
            and l.ends_at >= @CheckedInAt
        on conflict (organization_id, lesson_id, client_id) do nothing
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId,
            checked_in_at as AttendanceDate, status::text as Status, validated_at as ValidatedAt,
            validated_by_person_id as ValidatedByPersonId, created_at as CreatedAt, updated_at as UpdatedAt;";

    public const string ListByOrganization = @"
        select
            a.id,
            a.organization_id as OrganizationId,
            a.lesson_id as LessonId,
            a.client_id as ClientId,
            p.full_name as ClientName,
            a.checked_in_at as AttendanceDate,
            a.status::text as Status,
            a.validated_at as ValidatedAt,
            a.validated_by_person_id as ValidatedByPersonId,
            a.created_at as CreatedAt,
            a.updated_at as UpdatedAt
        from attendances a
        join clients c on c.id = a.client_id and c.organization_id = a.organization_id
        join persons p on p.id = c.person_id
        where a.organization_id = @OrganizationId
        order by a.created_at desc
        offset @Offset rows fetch next @PageSize rows only;";

    public const string GetById = @"
        select
            a.id,
            a.organization_id as OrganizationId,
            a.lesson_id as LessonId,
            a.client_id as ClientId,
            p.full_name as ClientName,
            a.checked_in_at as AttendanceDate,
            a.status::text as Status,
            a.validated_at as ValidatedAt,
            a.validated_by_person_id as ValidatedByPersonId,
            a.created_at as CreatedAt,
            a.updated_at as UpdatedAt
        from attendances a
        join clients c on c.id = a.client_id and c.organization_id = a.organization_id
        join persons p on p.id = c.person_id
        where a.organization_id = @OrganizationId and a.id = @Id;";

    public const string Update = @"
        update attendances
        set
            lesson_id = @LessonId,
            client_id = @ClientId,
            checked_in_at = @AttendanceDate,
            status = @Status,
            validated_at = @ValidatedAt,
            validated_by_person_id = @ValidatedByPersonId,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId,
            checked_in_at as AttendanceDate, status::text as Status, validated_at as ValidatedAt,
            validated_by_person_id as ValidatedByPersonId, created_at as CreatedAt, updated_at as UpdatedAt;";

    public const string Delete = @"
        delete from attendances
        where organization_id = @OrganizationId and id = @Id;";

    public const string ListByLesson = @"
        select
            a.id,
            a.organization_id as OrganizationId,
            a.lesson_id as LessonId,
            a.client_id as ClientId,
            p.full_name as ClientName,
            a.checked_in_at as AttendanceDate,
            a.status::text as Status,
            a.validated_at as ValidatedAt,
            a.validated_by_person_id as ValidatedByPersonId,
            a.created_at as CreatedAt,
            a.updated_at as UpdatedAt
        from attendances a
        join clients c on c.id = a.client_id and c.organization_id = a.organization_id
        join persons p on p.id = c.person_id
        where a.organization_id = @OrganizationId and a.lesson_id = @LessonId
        order by a.created_at, a.client_id;";

    public const string ListEligibleClientsByLesson = @"
        select c.id as ClientId, person.full_name as ClientName
        from lessons l
        join clients c on c.organization_id = l.organization_id
            and c.current_status = 'active'
        join persons person on person.id = c.person_id
            and person.deleted_at is null
        where l.organization_id = @OrganizationId
          and l.id = @LessonId
          and exists (
              select 1 from person_modalities pm
              join modalities modality on modality.id = pm.modality_id
                  and modality.organization_id = l.organization_id
                  and modality.is_active = true
              where pm.person_id = c.person_id
                and pm.modality_id = l.modality_id
          )
        order by person.full_name;";

    public const string ListPendingByOrganization = @"
        select
            a.id,
            a.organization_id as OrganizationId,
            a.lesson_id as LessonId,
            a.client_id as ClientId,
            p.full_name as ClientName,
            a.checked_in_at as AttendanceDate,
            a.status::text as Status,
            a.validated_at as ValidatedAt,
            a.validated_by_person_id as ValidatedByPersonId,
            a.created_at as CreatedAt,
            a.updated_at as UpdatedAt
        from attendances a
        join clients c on c.id = a.client_id and c.organization_id = a.organization_id
        join persons p on p.id = c.person_id
        where a.organization_id = @OrganizationId
            and a.status = 'pending_confirmation'
        order by a.checked_in_at, p.full_name
        offset @Offset rows fetch next @PageSize rows only;";

    public const string ValidateCheckIn = @"
        update attendances
        set status = @Status::attendance_status,
            validated_at = @ValidatedAt,
            validated_by_person_id = @ValidatedByPersonId,
            updated_at = @ValidatedAt
        where organization_id = @OrganizationId
            and id = @AttendanceId
            and status = 'pending_confirmation'
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId,
            checked_in_at as AttendanceDate, status::text as Status, validated_at as ValidatedAt,
            validated_by_person_id as ValidatedByPersonId, created_at as CreatedAt, updated_at as UpdatedAt;";

    public const string LockLessonForCall = @"
        select l.id
        from lessons l
        where l.organization_id = @OrganizationId
          and l.id = @LessonId
          and l.status in ('scheduled', 'completed')
        for update;";

    public const string UpsertCallEntry = @"
        insert into attendances (
            id, organization_id, lesson_id, client_id, status, checked_in_at,
            validated_at, validated_by_person_id, created_at
        )
        select
            @Id, @OrganizationId, l.id, c.id, @Status::attendance_status, @AttendanceDate,
            @ValidatedAt, @ValidatedByPersonId, @CreatedAt
        from lessons l
        join clients c
          on c.id = @ClientId
         and c.organization_id = l.organization_id
         and c.current_status = 'active'
        where l.organization_id = @OrganizationId
          and l.id = @LessonId
          and l.status in ('scheduled', 'completed')
          and exists (
              select 1 from person_modalities pm
              join modalities modality on modality.id = pm.modality_id
                  and modality.organization_id = l.organization_id
                  and modality.is_active = true
              where pm.person_id = c.person_id
                and pm.modality_id = l.modality_id
          )
        on conflict (organization_id, lesson_id, client_id)
        do update set
            status = excluded.status,
            checked_in_at = excluded.checked_in_at,
            validated_at = excluded.validated_at,
            validated_by_person_id = excluded.validated_by_person_id,
            updated_at = now()
        returning
            id,
            organization_id as OrganizationId,
            lesson_id as LessonId,
            client_id as ClientId,
            checked_in_at as AttendanceDate,
            status::text as Status,
            validated_at as ValidatedAt,
            validated_by_person_id as ValidatedByPersonId,
            created_at as CreatedAt,
            updated_at as UpdatedAt;";
}
