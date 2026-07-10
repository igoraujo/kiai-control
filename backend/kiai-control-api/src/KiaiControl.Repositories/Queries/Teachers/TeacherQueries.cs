namespace KiaiControl.Repositories.Queries.Teachers;

public static class TeacherQueries
{
    public const string Insert = """
        with inserted_person as (
            insert into persons (id, organization_id, full_name, status, hide_from_ranking, created_at)
            values (@PersonId, @OrganizationId, @FullName, @Status, false, @CreatedAt)
            returning id
        ), inserted_teacher as (
            insert into teachers (id, organization_id, person_id, current_status, created_at)
            values (@Id, @OrganizationId, @PersonId, @Status, @CreatedAt)
            returning id, organization_id, person_id, current_status, created_at, updated_at
        )
        select
            t.id,
            t.organization_id as OrganizationId,
            t.person_id as PersonId,
            p.full_name as FullName,
            t.current_status as Status,
            t.created_at as CreatedAt,
            t.updated_at as UpdatedAt
        from inserted_teacher t
        join persons p on p.id = t.person_id;
        """;

    public const string ListByOrganization = """
        select
            t.id,
            t.organization_id as OrganizationId,
            t.person_id as PersonId,
            p.full_name as FullName,
            t.current_status as Status,
            t.created_at as CreatedAt,
            t.updated_at as UpdatedAt
        from teachers t
        join persons p on p.id = t.person_id
        where t.organization_id = @OrganizationId and p.deleted_at is null
        order by t.created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        """;

    public const string GetById = """
        select
            t.id,
            t.organization_id as OrganizationId,
            t.person_id as PersonId,
            p.full_name as FullName,
            t.current_status as Status,
            t.created_at as CreatedAt,
            t.updated_at as UpdatedAt
        from teachers t
        join persons p on p.id = t.person_id
        where t.organization_id = @OrganizationId and t.id = @Id and p.deleted_at is null;
        """;

    public const string Update = """
        with target as (
            select t.id, t.organization_id, t.person_id
            from teachers t
            where t.organization_id = @OrganizationId and t.id = @Id
        ), updated_person as (
            update persons p
            set
                full_name = @FullName,
                status = @Status,
                updated_at = @UpdatedAt
            from target t
            where p.id = t.person_id
            returning p.id
        ), updated_teacher as (
            update teachers t
            set
                current_status = @Status,
                updated_at = @UpdatedAt
            from target x
            where t.id = x.id
            returning t.id, t.organization_id, t.person_id, t.current_status, t.created_at, t.updated_at
        )
        select
            t.id,
            t.organization_id as OrganizationId,
            t.person_id as PersonId,
            p.full_name as FullName,
            t.current_status as Status,
            t.created_at as CreatedAt,
            t.updated_at as UpdatedAt
        from updated_teacher t
        join persons p on p.id = t.person_id;
        """;

    public const string Delete = """
        delete from teachers
        where organization_id = @OrganizationId and id = @Id;
        """;
}
