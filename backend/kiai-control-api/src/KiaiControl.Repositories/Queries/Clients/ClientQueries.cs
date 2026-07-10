namespace KiaiControl.Repositories.Queries.Clients;

public static class ClientQueries
{
    public const string Insert = """
        with inserted_person as (
            insert into persons (id, organization_id, full_name, status, hide_from_ranking, created_at)
            values (@PersonId, @OrganizationId, @FullName, @Status, false, @CreatedAt)
            returning id
        ), inserted_client as (
            insert into clients (id, organization_id, person_id, current_status, created_at)
            values (@Id, @OrganizationId, @PersonId, @Status, @CreatedAt)
            returning id, organization_id, person_id, current_status, created_at, updated_at
        )
        select
            c.id,
            c.organization_id as OrganizationId,
            c.person_id as PersonId,
            p.full_name as FullName,
            c.current_status as Status,
            c.created_at as CreatedAt,
            c.updated_at as UpdatedAt
        from inserted_client c
        join persons p on p.id = c.person_id;
        """;

    public const string ListByOrganization = """
        select
            c.id,
            c.organization_id as OrganizationId,
            c.person_id as PersonId,
            p.full_name as FullName,
            c.current_status as Status,
            c.created_at as CreatedAt,
            c.updated_at as UpdatedAt
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId and p.deleted_at is null
        order by c.created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        """;

    public const string GetById = """
        select
            c.id,
            c.organization_id as OrganizationId,
            c.person_id as PersonId,
            p.full_name as FullName,
            c.current_status as Status,
            c.created_at as CreatedAt,
            c.updated_at as UpdatedAt
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId and c.id = @Id and p.deleted_at is null;
        """;

    public const string Update = """
        with target as (
            select c.id, c.organization_id, c.person_id
            from clients c
            where c.organization_id = @OrganizationId and c.id = @Id
        ), updated_person as (
            update persons p
            set
                full_name = @FullName,
                status = @Status,
                updated_at = @UpdatedAt
            from target t
            where p.id = t.person_id
            returning p.id
        ), updated_client as (
            update clients c
            set
                current_status = @Status,
                updated_at = @UpdatedAt
            from target t
            where c.id = t.id
            returning c.id, c.organization_id, c.person_id, c.current_status, c.created_at, c.updated_at
        )
        select
            c.id,
            c.organization_id as OrganizationId,
            c.person_id as PersonId,
            p.full_name as FullName,
            c.current_status as Status,
            c.created_at as CreatedAt,
            c.updated_at as UpdatedAt
        from updated_client c
        join persons p on p.id = c.person_id;
        """;

    public const string Delete = """
        delete from clients
        where organization_id = @OrganizationId and id = @Id;
        """;
}
