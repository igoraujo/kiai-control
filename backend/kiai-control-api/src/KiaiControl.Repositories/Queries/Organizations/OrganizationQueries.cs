namespace KiaiControl.Repositories.Queries.Organizations;

public static class OrganizationQueries
{
    public const string Register = @"
        with inserted_organization as (
            insert into organizations (id, name, trade_name, document, email, phone, status, created_at)
            values (@Id, @Name, @TradeName, @Document, @Email, @Phone, 'active', @CreatedAt)
            on conflict do nothing
            returning id, name, trade_name, document, email, phone, status::text as status, created_at, updated_at
        ), inserted_settings as (
            insert into organization_settings (id, organization_id)
            select gen_random_uuid(), id from inserted_organization
            returning organization_id
        ), inserted_person as (
            insert into persons (id, organization_id, full_name, email, status, hide_from_ranking, created_at)
            select @PersonId, id, @OwnerFullName, @OwnerEmail, 'active', false, @CreatedAt
            from inserted_organization
            returning id, organization_id
        ), inserted_account as (
            insert into user_accounts (id, person_id, email, password_hash, is_active, created_at)
            select @OwnerUserId, id, @OwnerEmail, @OwnerPasswordHash, true, @CreatedAt
            from inserted_person
            returning id, person_id
        ), assigned_owner_role as (
            insert into person_roles (person_id, role_id, granted_at)
            select account.person_id, role.id, @CreatedAt
            from inserted_account account
            cross join roles role
            where role.code = 'owner'
            returning person_id
        )
        select
            org.id,
            org.name,
            org.trade_name as TradeName,
            org.document,
            org.email,
            org.phone,
            org.status,
            org.created_at as CreatedAt,
            org.updated_at as UpdatedAt,
            account.id as OwnerUserId
        from inserted_organization org
        join inserted_settings settings on settings.organization_id = org.id
        join inserted_account account on true
        join assigned_owner_role owner_role on owner_role.person_id = account.person_id;
        ";

    public const string GetById = @"
        select
            id,
            name,
            trade_name as TradeName,
            document,
            email,
            phone,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from organizations
        where id = @OrganizationId and deleted_at is null;
        ";

    public const string Update = @"
        update organizations
        set
            name = @Name,
            trade_name = @TradeName,
            document = @Document,
            email = @Email,
            phone = @Phone,
            updated_at = @UpdatedAt
        where id = @Id and deleted_at is null
        returning
            id,
            name,
            trade_name as TradeName,
            document,
            email,
            phone,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";
}