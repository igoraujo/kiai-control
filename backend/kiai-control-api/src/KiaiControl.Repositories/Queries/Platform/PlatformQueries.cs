namespace KiaiControl.Repositories.Queries.Platform;

public static class PlatformQueries
{
    public const string Organizations = """
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
        where (@Archive = 'all'
                or (@Archive = 'active' and deleted_at is null)
                or (@Archive = 'archived' and deleted_at is not null))
            and id <> @PlatformOrganizationId
            and (@Search is null or strpos(lower(concat_ws(' ', name, trade_name, document, email)), lower(@Search)) > 0)
            and (@Status is null or status::text = @Status)
        order by lower(name), id
        offset @Offset
        limit @PageSize;

        select count(1)
        from organizations
        where (@Archive = 'all'
                or (@Archive = 'active' and deleted_at is null)
                or (@Archive = 'archived' and deleted_at is not null))
            and id <> @PlatformOrganizationId
            and (@Search is null or strpos(lower(concat_ws(' ', name, trade_name, document, email)), lower(@Search)) > 0)
            and (@Status is null or status::text = @Status);
        """;

    public const string LockOrganization = """
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
        from organizations o
        where id = @OrganizationId
            and id <> @PlatformOrganizationId
            and (@AllowArchived or deleted_at is null)
            and not exists (
                select 1
                from persons p
                join user_accounts u on u.person_id = p.id
                where p.organization_id = o.id and u.is_platform_owner
            )
        for update;
        """;

    public const string Status = """
        update organizations
        set status = cast(@Status as organization_status), updated_at = now()
        where id = @OrganizationId and deleted_at is null;

        update user_accounts u
        set session_version = session_version + 1, updated_at = now()
        from persons p
        where u.person_id = p.id and p.organization_id = @OrganizationId;

        update refresh_tokens token
        set revoked_at = now()
        from user_accounts u
        join persons p on p.id = u.person_id
        where token.user_account_id = u.id
            and p.organization_id = @OrganizationId and token.revoked_at is null;

        update password_reset_tokens token
        set revoked_at = now()
        from user_accounts u
        join persons p on p.id = u.person_id
        where token.user_account_id = u.id and p.organization_id = @OrganizationId
            and token.revoked_at is null and token.used_at is null;
        """;

    public const string Accounts = """
        select
            u.id as UserId,
            p.id as PersonId,
            p.full_name as FullName,
            u.email,
            u.is_active as IsActive,
            u.must_change_password as MustChangePassword,
            coalesce((
                select array_agg(r.code::text order by r.code)
                from person_roles pr
                join roles r on r.id = pr.role_id
                where pr.person_id = p.id
            ), array[]::text[]) as Roles
        from user_accounts u
        join persons p on p.id = u.person_id
        where p.organization_id = @OrganizationId
            and p.deleted_at is null
            and not u.is_platform_owner
            and (@Search is null or strpos(lower(concat_ws(' ', p.full_name, u.email)), lower(@Search)) > 0)
        order by lower(p.full_name), u.id
        offset @Offset
        limit @PageSize;

        select count(1)
        from user_accounts u
        join persons p on p.id = u.person_id
        where p.organization_id = @OrganizationId and p.deleted_at is null
            and not u.is_platform_owner
            and (@Search is null or strpos(lower(concat_ws(' ', p.full_name, u.email)), lower(@Search)) > 0);
        """;

    public const string LockAccount = """
        select
            u.id as UserId,
            p.id as PersonId,
            p.full_name as FullName,
            u.email,
            u.is_active as IsActive,
            u.must_change_password as MustChangePassword,
            coalesce((
                select array_agg(r.code::text order by r.code)
                from person_roles pr
                join roles r on r.id = pr.role_id
                where pr.person_id = p.id
            ), array[]::text[]) as Roles
        from user_accounts u
        join persons p on p.id = u.person_id
        where p.organization_id = @OrganizationId
            and p.deleted_at is null
            and u.id = @UserId
            and u.id <> @ActorUserId
            and not u.is_platform_owner
        for update of u, p;
        """;

    public const string CreateAccount = """
        insert into persons (id, organization_id, full_name, email, status)
        values (@PersonId, @OrganizationId, @FullName, @Email, 'active');

        insert into user_accounts (id, person_id, email, password_hash, is_active, must_change_password)
        values (@UserId, @PersonId, @Email, @PasswordHash, true, true);

        insert into person_roles (person_id, role_id, granted_by_user_account_id)
        select @PersonId, id, @ActorUserId from roles where code::text = @Role;
        """;

    public const string OtherActiveOwners = """
        select count(1)
        from user_accounts u
        join persons p on p.id = u.person_id
        join person_roles pr on pr.person_id = p.id
        join roles r on r.id = pr.role_id
        where p.organization_id = @OrganizationId and p.deleted_at is null
            and p.status = 'active' and u.is_active and u.id <> @UserId and r.code = 'owner';
        """;

    public const string ProfilesValid = """
        select
            (not ('client' = any(@Roles)) or exists (
                select 1 from clients where person_id = @PersonId and organization_id = @OrganizationId
            )) and (not ('teacher' = any(@Roles)) or exists (
                select 1 from teachers where person_id = @PersonId and organization_id = @OrganizationId
            ));
        """;

    public const string UpdateAccount = """
        update user_accounts
        set email = @Email, is_active = @Active, updated_at = now()
        where id = @UserId;

        update persons
        set email = @Email, updated_at = now()
        where id = @PersonId and organization_id = @OrganizationId;

        delete from person_roles where person_id = @PersonId;

        insert into person_roles (person_id, role_id, granted_by_user_account_id)
        select @PersonId, id, @ActorUserId from roles where code::text = any(@Roles);
        """;

    public const string ResetPassword = """
        update user_accounts
        set password_hash = @PasswordHash, must_change_password = true, updated_at = now()
        where id = @UserId;
        """;

    public const string RevokeSessions = """
        update user_accounts
        set session_version = session_version + 1, updated_at = now()
        where id = @UserId;

        update refresh_tokens set revoked_at = now()
        where user_account_id = @UserId and revoked_at is null;

        update password_reset_tokens set revoked_at = now()
        where user_account_id = @UserId and revoked_at is null and used_at is null;
        """;

    public const string Audit = """
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
        where (@OrganizationId is null or a.organization_id = @OrganizationId)
            and (@ActorUserId is null or a.actor_user_account_id = @ActorUserId)
            and (@ActorSearch is null or strpos(lower(concat_ws(' ', p.full_name, u.email)), lower(@ActorSearch)) > 0)
            and (@Action is null or a.action = @Action)
            and a.created_at >= @From
            and a.created_at < @To
        order by a.created_at desc, a.id desc
        offset @Offset
        limit @PageSize;

        select count(1)
        from audit_logs a
        left join persons p on p.id = a.actor_person_id
        left join user_accounts u on u.id = a.actor_user_account_id
        where (@OrganizationId is null or a.organization_id = @OrganizationId)
            and (@ActorUserId is null or a.actor_user_account_id = @ActorUserId)
            and (@ActorSearch is null or strpos(lower(concat_ws(' ', p.full_name, u.email)), lower(@ActorSearch)) > 0)
            and (@Action is null or a.action = @Action)
            and a.created_at >= @From
            and a.created_at < @To;
        """;

    public const string Incidents = """
        select
            i.id,
            i.organization_id as OrganizationId,
            o.name as OrganizationName,
            i.actor_user_id as ActorUserId,
            i.category,
            i.method,
            i.route,
            i.status_code as StatusCode,
            i.correlation_id as CorrelationId,
            i.created_at as CreatedAt
        from platform_incidents i
        left join organizations o on o.id = i.organization_id
        where (@OrganizationId is null or i.organization_id = @OrganizationId)
            and (@Action is null or i.category = @Action)
            and i.created_at >= @From
            and i.created_at < @To
        order by i.created_at desc, i.id desc
        offset @Offset
        limit @PageSize;

        select count(1)
        from platform_incidents i
        where (@OrganizationId is null or i.organization_id = @OrganizationId)
            and (@Action is null or i.category = @Action)
            and i.created_at >= @From
            and i.created_at < @To;
        """;

    public const string InsertIncident = """
        insert into platform_incidents (
            id, organization_id, actor_user_id, category, method, route, status_code, correlation_id, created_at
        )
        values (@Id, @OrganizationId, @ActorUserId, @Category, @Method, @Route, @StatusCode, @CorrelationId, @CreatedAt);
        """;

    public const string Overview = """
        select
            count(1)::int as Organizations,
            count(1) filter (where status = 'active')::int as ActiveOrganizations,
            count(1) filter (where status = 'suspended')::int as SuspendedOrganizations,
            (select count(1)::int from platform_incidents where created_at >= now() - interval '24 hours' and category = 'request_failure') as RequestFailures,
            (select count(1)::int from platform_incidents where created_at >= now() - interval '24 hours' and category = 'authentication_failure') as AuthenticationFailures,
            (select count(1)::int from platform_incidents where created_at >= now() - interval '24 hours' and category = 'audit_failure') as AuditFailures
        from organizations
        where deleted_at is null and id <> @PlatformOrganizationId;
        """;
}
