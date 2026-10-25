namespace KiaiControl.Repositories.Queries.Branches;

public static class BranchQueries
{
    public const string Insert = @"
        insert into branches (id, organization_id, name, address_line, city, state, postal_code, created_at)
        select @Id, o.id, @Name, @AddressLine, @City, @State, @PostalCode, @CreatedAt
        from organizations o
        where o.id = @OrganizationId and o.deleted_at is null
        returning
            id,
            organization_id as OrganizationId,
            name,
            address_line as AddressLine,
            city,
            state,
            postal_code as PostalCode,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            address_line as AddressLine,
            city,
            state,
            postal_code as PostalCode,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from branches
        where organization_id = @OrganizationId
        order by name
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            address_line as AddressLine,
            city,
            state,
            postal_code as PostalCode,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from branches
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string Update = @"
        update branches
        set
            name = @Name,
            address_line = @AddressLine,
            city = @City,
            state = @State,
            postal_code = @PostalCode,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning
            id,
            organization_id as OrganizationId,
            name,
            address_line as AddressLine,
            city,
            state,
            postal_code as PostalCode,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string Delete = @"
        delete from branches
        where organization_id = @OrganizationId and id = @Id;
        ";
}