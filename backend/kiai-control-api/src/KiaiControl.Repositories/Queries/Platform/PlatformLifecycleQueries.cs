namespace KiaiControl.Repositories.Queries.Platform;

public static class PlatformLifecycleQueries
{
    public const string GetOrganization = """
        select
            id,
            name,
            trade_name as TradeName,
            document,
            email,
            phone,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt,
            deleted_at as DeletedAt,
            privacy_hold as PrivacyHold
        from organizations
        where id = @OrganizationId
            and id <> @PlatformOrganizationId;
        """;

    public const string Archive = """
        update organizations
        set
            deleted_at = case when @Archived then now() else null end,
            status = cast(@Status as organization_status),
            updated_at = now()
        where id = @OrganizationId;
        """;

    public const string PrivacyHold = """
        update organizations
        set privacy_hold = @Hold, updated_at = now()
        where id = @OrganizationId;
        """;

    public const string AuditById = """
        select
            a.id,
            a.organization_id as OrganizationId,
            o.name as OrganizationName,
            a.actor_person_id as ActorPersonId,
            a.actor_user_account_id as ActorUserAccountId,
            p.full_name as ActorName,
            u.email as ActorEmail,
            a.action,
            a.entity_name as EntityName,
            a.entity_id as EntityId,
            a.metadata::text as Metadata,
            a.ip_address as IpAddress,
            a.created_at as CreatedAt
        from audit_logs a
        join organizations o on o.id = a.organization_id
        left join persons p on p.id = a.actor_person_id
        left join user_accounts u on u.id = a.actor_user_account_id
        where a.id = @AuditId and a.organization_id = @OrganizationId;
        """;
}
