namespace KiaiControl.Repositories.Queries.Audit;

public static class AuditQueries
{
    public const string Insert = @"
        insert into audit_logs (
            id, organization_id, actor_person_id, actor_user_account_id, action, entity_name,
            entity_id, metadata, ip_address, user_agent, created_at
        )
        values (
            @Id, @OrganizationId, @ActorPersonId, @ActorUserAccountId, @Action, @EntityName,
            @EntityId, @Metadata::jsonb, @IpAddress, @UserAgent, @CreatedAt
        );
        ";

    public const string List = @"
        select
            id,
            organization_id as OrganizationId,
            actor_person_id as ActorPersonId,
            actor_user_account_id as ActorUserAccountId,
            action,
            entity_name as EntityName,
            entity_id as EntityId,
            metadata::text as Metadata,
            ip_address as IpAddress,
            user_agent as UserAgent,
            created_at as CreatedAt
        from audit_logs
        where organization_id = @OrganizationId
          and (@From is null or created_at >= @From)
          and (@To is null or created_at < @To)
        order by created_at desc, id desc
        offset @Offset rows fetch next @PageSize rows only;
        ";
}