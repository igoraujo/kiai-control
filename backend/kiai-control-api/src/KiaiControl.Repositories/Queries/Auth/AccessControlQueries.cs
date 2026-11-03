namespace KiaiControl.Repositories.Queries.Auth;

public static class AccessControlQueries
{
    public const string ListPeopleWithoutAccounts = @"
        select person.id as PersonId, person.full_name as FullName, person.email as Email
        from persons person
        where person.organization_id = @OrganizationId
          and person.deleted_at is null
          and not exists (
              select 1 from user_accounts account where account.person_id = person.id
          )
        order by person.full_name;
        ";

    public const string CreateUserAccount = @"
        with target_person as (
            update persons person
            set email = @Email
            where person.id = @PersonId
              and person.organization_id = @OrganizationId
              and person.deleted_at is null
              and not exists (
                  select 1 from user_accounts account where account.person_id = person.id
              )
            returning person.id
        ), created_account as (
            insert into user_accounts (
                id, person_id, email, password_hash, is_active, is_platform_owner,
                must_change_password, created_at
            )
            select @UserId, target_person.id, @Email, @PasswordHash, true, false, true, now()
            from target_person
            returning person_id
        ), assigned_role as (
            insert into person_roles (person_id, role_id, granted_by_user_account_id, granted_at)
            select created_account.person_id, role.id, @GrantedByUserId, now()
            from created_account
            cross join roles role
            where role.code = @RoleCode::role_code
            on conflict (person_id, role_id) do update
            set granted_by_user_account_id = excluded.granted_by_user_account_id,
                granted_at = excluded.granted_at
            returning person_id
        )
        select person_id from assigned_role;
        ";

    public const string ListRoles = @"
        select id, code::text as Code, name, description
        from roles order by code;
        ";

    public const string ListPermissions = @"
        select id, code, name, description
        from permissions order by code;
        ";

    public const string ListPersonRoles = @"
        select role.code::text
        from person_roles assignment
        join persons person on person.id = assignment.person_id
        join roles role on role.id = assignment.role_id
        where person.organization_id = @OrganizationId
          and person.id = @PersonId
          and person.deleted_at is null
        order by role.code;
        ";

    public const string AssignPersonRole = @"
        insert into person_roles (person_id, role_id, granted_by_user_account_id, granted_at)
        select person.id, role.id, @GrantedByUserId, now()
        from persons person
        join roles role on role.code = @RoleCode::role_code
        where person.id = @PersonId
          and person.organization_id = @OrganizationId
          and person.deleted_at is null
        on conflict (person_id, role_id)
        do update set granted_by_user_account_id = excluded.granted_by_user_account_id, granted_at = now()
        returning person_id;
        ";

    public const string RevokePersonRole = @"
        delete from person_roles assignment
        using persons person, roles role
        where assignment.person_id = person.id
          and assignment.role_id = role.id
          and person.id = @PersonId
          and person.organization_id = @OrganizationId
          and role.code = @RoleCode::role_code
          and person.deleted_at is null;
        ";

    public const string ListRolePermissions = @"
        select permission.id, permission.code, permission.name, permission.description
      from roles role
      cross join permissions permission
      left join role_permissions base_grant
        on base_grant.role_id = role.id and base_grant.permission_id = permission.id
      left join organization_role_permissions organization_grant
        on organization_grant.organization_id = @OrganizationId
        and organization_grant.role_id = role.id
        and organization_grant.permission_id = permission.id
      where role.code = @RoleCode::role_code
        and coalesce(organization_grant.is_granted, base_grant.role_id is not null)
        order by permission.code;
        ";

    public const string GrantRolePermission = @"
      insert into organization_role_permissions (
        organization_id, role_id, permission_id, is_granted, changed_at, changed_by_user_account_id
      )
      select @OrganizationId, role.id, permission.id, true, now(), @ActorUserId
        from roles role cross join permissions permission
        where role.code = @RoleCode::role_code and permission.code = @PermissionCode
      on conflict (organization_id, role_id, permission_id) do update
      set is_granted = true,
        changed_at = now(),
        changed_by_user_account_id = excluded.changed_by_user_account_id
        returning role_id;
        ";

    public const string RevokeRolePermission = @"
      insert into organization_role_permissions (
        organization_id, role_id, permission_id, is_granted, changed_at, changed_by_user_account_id
      )
      select @OrganizationId, role.id, permission.id, false, now(), @ActorUserId
      from roles role cross join permissions permission
      where role.code = @RoleCode::role_code and permission.code = @PermissionCode
      on conflict (organization_id, role_id, permission_id) do update
      set is_granted = false,
        changed_at = now(),
        changed_by_user_account_id = excluded.changed_by_user_account_id
      returning role_id;
        ";
}