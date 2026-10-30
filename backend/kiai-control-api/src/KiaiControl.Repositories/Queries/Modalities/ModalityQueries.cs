namespace KiaiControl.Repositories.Queries.Modalities;

public static class ModalityQueries
{
    public const string Insert = @"
        insert into modalities (id, organization_id, name, type, description, is_active, created_at)
        values (@Id, @OrganizationId, @Name, @Type::modality_type, @Description, true, @CreatedAt)
        returning
            id,
            organization_id as OrganizationId,
            name,
            type::text as Type,
            description,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            type::text as Type,
            description,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from modalities
        where organization_id = @OrganizationId
        order by name
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            type::text as Type,
            description,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from modalities
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string Update = @"
        update modalities
        set
            name = @Name,
            type = @Type::modality_type,
            description = @Description,
            is_active = @IsActive,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning
            id,
            organization_id as OrganizationId,
            name,
            type::text as Type,
            description,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string Deactivate = @"
        update modalities
        set is_active = false, updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id and is_active = true;
        ";

    public const string ListClientModalities = @"
        select
            modality.id,
            modality.organization_id as OrganizationId,
            modality.name,
            modality.type::text as Type,
            modality.description,
            modality.is_active as IsActive,
            modality.created_at as CreatedAt,
            modality.updated_at as UpdatedAt
        from clients client
        join persons person on person.id = client.person_id
            and person.organization_id = client.organization_id
            and person.deleted_at is null
        join person_modalities assignment on assignment.person_id = person.id
        join modalities modality on modality.id = assignment.modality_id
            and modality.organization_id = client.organization_id
        where client.organization_id = @OrganizationId
          and client.id = @ClientId
        order by modality.name;
        ";

        public const string LinkClient = @"
                insert into person_modalities (person_id, modality_id, linked_at)
                select p.id, m.id, now()
                from clients c
                join persons p on p.id = c.person_id and p.organization_id = c.organization_id
                join modalities m on m.id = @ModalityId and m.organization_id = c.organization_id and m.is_active = true
                where c.id = @ClientId and c.organization_id = @OrganizationId and c.current_status = 'active'
                on conflict (person_id, modality_id) do update set linked_at = person_modalities.linked_at;
                ";

        public const string UnlinkClient = @"
                delete from person_modalities pm
                using clients c, modalities m
                where pm.person_id = c.person_id
                    and pm.modality_id = m.id
                    and c.id = @ClientId
                    and c.organization_id = @OrganizationId
                    and m.id = @ModalityId
                    and m.organization_id = @OrganizationId;
                ";
}