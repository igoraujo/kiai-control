namespace KiaiControl.Repositories.Queries.Platform;

public static class PlatformBootstrapQueries
{
    public const string ExistingOwner = """
        select email from user_accounts where id = @UserId and is_platform_owner;
        """;

    public const string ExistingEmail = """
        select id from user_accounts where lower(email) = @Email;
        """;

    public const string Create = """
        insert into organizations (id, name, status, created_at)
        values (@OrganizationId, 'Kiai Control Platform', 'active', @CreatedAt)
        on conflict (id) do nothing;

        insert into organization_settings (id, organization_id)
        values (gen_random_uuid(), @OrganizationId)
        on conflict (organization_id) do nothing;

        insert into persons (id, organization_id, full_name, email, status, created_at)
        values (@PersonId, @OrganizationId, @FullName, @Email, 'active', @CreatedAt)
        on conflict (id) do nothing;

        insert into user_accounts (
            id, person_id, email, password_hash, is_active, is_platform_owner,
            must_change_password, created_at
        )
        values (@UserId, @PersonId, @Email, @PasswordHash, true, true, true, @CreatedAt);
        """;
}
