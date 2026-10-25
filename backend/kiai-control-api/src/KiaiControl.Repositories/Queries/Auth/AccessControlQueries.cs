namespace KiaiControl.Repositories.Queries.Auth;

public static class AccessControlQueries
{
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
        from role_permissions assignment
        join roles role on role.id = assignment.role_id
        join permissions permission on permission.id = assignment.permission_id
        where role.code = @RoleCode::role_code
        order by permission.code;
        ";

    public const string GrantRolePermission = @"
        insert into role_permissions (role_id, permission_id, granted_at)
        select role.id, permission.id, now()
        from roles role cross join permissions permission
        where role.code = @RoleCode::role_code and permission.code = @PermissionCode
        on conflict (role_id, permission_id) do nothing
        returning role_id;
        ";

    public const string RevokeRolePermission = @"
        delete from role_permissions assignment
        using roles role, permissions permission
        where assignment.role_id = role.id
          and assignment.permission_id = permission.id
          and role.code = @RoleCode::role_code
          and permission.code = @PermissionCode;
        ";
}