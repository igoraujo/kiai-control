namespace KiaiControl.Repositories.Queries.Attendance;

public static class AttendanceQueries
{
    public const string Insert = """
        insert into attendances (id, organization_id, lesson_id, client_id, status, checked_in_at, created_at)
        values (@Id, @OrganizationId, @LessonId, @ClientId, @Status, @AttendanceDate, @CreatedAt)
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId, checked_in_at as AttendanceDate, status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string ListByOrganization = """
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
        """;

    public const string GetById = """
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
        """;

    public const string Update = """
        update attendances
        set
            lesson_id = @LessonId,
            client_id = @ClientId,
            checked_in_at = @AttendanceDate,
            status = @Status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, lesson_id as LessonId, client_id as ClientId, checked_in_at as AttendanceDate, status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string Delete = """
        delete from attendances
        where organization_id = @OrganizationId and id = @Id;
        """;
}
