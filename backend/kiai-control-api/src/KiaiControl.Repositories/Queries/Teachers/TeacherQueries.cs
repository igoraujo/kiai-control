namespace KiaiControl.Repositories.Queries.Teachers;

public static class TeacherQueries
{
    public const string Insert = """
        insert into teachers (id, organization_id, full_name, current_status, created_at)
        values (@Id, @OrganizationId, @FullName, @Status, @CreatedAt)
        returning id, organization_id as OrganizationId, full_name as FullName, current_status as Status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string ListByOrganization = """
        select
            id,
            organization_id as OrganizationId,
            full_name as FullName,
            current_status as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from teachers
        where organization_id = @OrganizationId
        order by created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        """;

    public const string GetById = """
        select
            id,
            organization_id as OrganizationId,
            full_name as FullName,
            current_status as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from teachers
        where organization_id = @OrganizationId and id = @Id;
        """;

    public const string Update = """
        update teachers
        set
            full_name = @FullName,
            current_status = @Status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, full_name as FullName, current_status as Status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string Delete = """
        delete from teachers
        where organization_id = @OrganizationId and id = @Id;
        """;
}
