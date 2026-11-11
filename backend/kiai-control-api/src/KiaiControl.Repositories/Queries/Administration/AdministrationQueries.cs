namespace KiaiControl.Repositories.Queries.Administration;

public static class AdministrationQueries
{
    public const string People = @"
        select
            p.id as PersonId,
            p.full_name as FullName,
            p.email,
            p.phone,
            p.status::text as Status,
            c.id as ClientId,
            t.id as TeacherId,
            u.id as UserId,
            u.email as LoginEmail,
            coalesce(u.is_active, false) as IsActive,
            coalesce(u.must_change_password, false) as MustChangePassword,
            coalesce(
                (
                    select array_agg(r.code::text order by r.code)
                    from person_roles pr
                    join roles r on r.id = pr.role_id
                    where pr.person_id = p.id
                ),
                array[]::text[]
            ) as Roles
        from persons p
        left join clients c on c.person_id = p.id
            and c.organization_id = p.organization_id
        left join teachers t on t.person_id = p.id
            and t.organization_id = p.organization_id
        left join user_accounts u on u.person_id = p.id
        where p.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Search is null
                or concat_ws(' ', p.full_name, p.email, p.phone, u.email) ilike @Search)
        order by
            lower(p.full_name),
            p.id
        offset @Offset
        limit @Size;

        select count(1)
        from persons p
        left join clients c on c.person_id = p.id
            and c.organization_id = p.organization_id
        left join teachers t on t.person_id = p.id
            and t.organization_id = p.organization_id
        left join user_accounts u on u.person_id = p.id
        where p.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Search is null
                or concat_ws(' ', p.full_name, p.email, p.phone, u.email) ilike @Search);
        ";

    public const string CreatePerson = @"
        insert into persons (id, organization_id, full_name, email, phone)
        values (@Id, @OrganizationId, @Name, @Email, @Phone)
        returning id;
        ";

    public const string UpdatePerson = @"
        update persons
        set
            full_name = @Name,
            email = @Email,
            phone = @Phone,
            updated_at = now()
        where id = @Id
            and organization_id = @OrganizationId
            and deleted_at is null
        returning id;
        ";

    public const string AddClient = @"
        insert into clients (id, organization_id, person_id, joined_at, current_status)
        select
            gen_random_uuid(),
            organization_id,
            id,
            current_date,
            'active'
        from persons
        where id = @PersonId
            and organization_id = @OrganizationId
            and deleted_at is null
        on conflict (person_id) do nothing;
        ";

    public const string AddTeacher = @"
        insert into teachers (id, organization_id, person_id, hired_at, active_from, current_status)
        select
            gen_random_uuid(),
            organization_id,
            id,
            current_date,
            current_date,
            'active'
        from persons
        where id = @PersonId
            and organization_id = @OrganizationId
            and deleted_at is null
        on conflict (person_id) do nothing;
        ";

    public const string AddRole = @"
        insert into person_roles (person_id, role_id, granted_by_user_account_id)
        select
            p.id,
            r.id,
            @ActorUserId
        from persons p
        cross join roles r
        where p.id = @PersonId
            and p.organization_id = @OrganizationId
            and p.deleted_at is null
            and r.code::text = @Profile
        on conflict do nothing;

        update user_accounts
        set session_version = session_version + 1
        where person_id = @PersonId;
        ";

    public const string LockManagedAccount = @"
        select u.id
        from user_accounts u
        join persons p on p.id = u.person_id
        where p.organization_id = @OrganizationId
            and p.id = @PersonId
            and p.deleted_at is null
            and u.id <> @ActorUserId
            and not u.is_platform_owner
            and (@ActorIsOwner
                or not exists (
                    select 1
                    from person_roles pr
                    join roles r on r.id = pr.role_id
                    where pr.person_id = p.id
                        and r.code in ('owner', 'admin')
                ))
        for update of u;
        ";

    public const string ManageAccount = @"
        update user_accounts
        set
            email = coalesce(@Email, email),
            is_active = coalesce(@Active, is_active),
            password_hash = coalesce(@PasswordHash, password_hash),
            must_change_password = case
                when @PasswordHash is not null then true
                else must_change_password
            end,
            session_version = session_version + 1,
            updated_at = now()
        where id = @UserId;

        update refresh_tokens
        set revoked_at = now()
        where user_account_id = @UserId
            and revoked_at is null;

        update password_reset_tokens
        set revoked_at = now()
        where user_account_id = @UserId
            and used_at is null
            and revoked_at is null;
        ";

    public const string GetSettings = @"
        select
            coalesce(settings.timezone, 'America/Sao_Paulo') as Timezone,
            coalesce(settings.currency_code, 'BRL') as CurrencyCode,
            coalesce(settings.allow_client_checkin, false) as AllowClientCheckin,
            coalesce(settings.require_teacher_confirmation, true) as RequireTeacherConfirmation,
            coalesce(settings.default_late_tolerance_minutes, 10) as DefaultLateToleranceMinutes
        from organizations organization
        left join organization_settings settings on settings.organization_id = organization.id
        where organization.id = @OrganizationId;
        ";

    public const string SaveSettings = @"
        insert into organization_settings (
            id,
            organization_id,
            timezone,
            currency_code,
            allow_client_checkin,
            require_teacher_confirmation,
            default_late_tolerance_minutes
        )
        values (
            gen_random_uuid(),
            @OrganizationId,
            @Timezone,
            @CurrencyCode,
            @AllowClientCheckin,
            @RequireTeacherConfirmation,
            @DefaultLateToleranceMinutes
        )
        on conflict (organization_id) do update
        set
            timezone = excluded.timezone,
            currency_code = excluded.currency_code,
            allow_client_checkin = excluded.allow_client_checkin,
            require_teacher_confirmation = excluded.require_teacher_confirmation,
            default_late_tolerance_minutes = excluded.default_late_tolerance_minutes,
            updated_at = now();
        ";

    public const string LockProfilePerson = @"
        select target.id
        from persons target
        where target.id = @PersonId
            and target.organization_id = @OrganizationId
            and target.deleted_at is null
            and (not exists (
                select 1
                from person_roles pr
                join roles r on r.id = pr.role_id
                where pr.person_id = target.id
                    and r.code in ('owner', 'admin')
            )
                or exists (
                    select 1
                    from user_accounts u
                    join persons p on p.id = u.person_id
                    join person_roles pr on pr.person_id = p.id
                    join roles r on r.id = pr.role_id
                    where u.id = @ActorUserId
                        and p.organization_id = @OrganizationId
                        and r.code = 'owner'
                ))
        for update of target;
        ";
}
