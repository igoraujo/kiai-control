begin;

insert into permissions (id, code, name, description)
values
    (gen_random_uuid(), 'access.manage', 'Manage access', 'List roles and manage person role assignments.'),
    (gen_random_uuid(), 'access.permissions.manage', 'Manage role permissions', 'Grant or revoke permissions on roles.'),
    (gen_random_uuid(), 'audit.read', 'Read audit log', 'Read organization audit events.'),
    (gen_random_uuid(), 'attendance.check-in', 'Check in to lessons', 'Allow a client to check in to their own lessons.'),
    (gen_random_uuid(), 'attendance.manage', 'Manage attendance', 'Record, review, and validate attendance.'),
    (gen_random_uuid(), 'billing.manage', 'Manage billing', 'Manage plans, invoices, and payments.'),
    (gen_random_uuid(), 'branches.manage', 'Manage branches', 'Manage organization branches.'),
    (gen_random_uuid(), 'class-groups.read', 'Read class groups', 'Read class groups, schedules, enrollments, and teachers.'),
    (gen_random_uuid(), 'class-groups.manage', 'Manage class groups', 'Manage class groups, schedules, and enrollments.'),
    (gen_random_uuid(), 'clients.manage', 'Manage clients', 'Manage client records and modality links.'),
    (gen_random_uuid(), 'fitness.manage', 'Manage fitness goals', 'Manage fitness goals and progress.'),
    (gen_random_uuid(), 'graduation.manage', 'Manage graduations', 'Manage graduation systems and client progression.'),
    (gen_random_uuid(), 'lessons.manage', 'Manage lessons', 'Manage lessons and lesson attendance.'),
    (gen_random_uuid(), 'modalities.manage', 'Manage modalities', 'Manage organization modalities.'),
    (gen_random_uuid(), 'notifications.read', 'Read notifications', 'Read and acknowledge the current user notifications.'),
    (gen_random_uuid(), 'notifications.manage', 'Manage notifications', 'Create and manage organization notifications.'),
    (gen_random_uuid(), 'organizations.manage', 'Manage organization', 'Manage organization settings and registration.'),
    (gen_random_uuid(), 'ranking.manage', 'Manage ranking', 'Manage ranking cycles, entries, and medals.'),
    (gen_random_uuid(), 'reports.read', 'Read reports', 'Read organization reports and dashboards.'),
    (gen_random_uuid(), 'teachers.manage', 'Manage teachers', 'Manage teacher records and assignments.')
on conflict (code) do update
set name = excluded.name,
    description = excluded.description;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where role.code = 'owner'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where role.code = 'admin'
  and permission.code <> 'access.permissions.manage'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where role.code in ('teacher', 'reception')
    and permission.code = 'class-groups.read'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where (role.code = 'client' and permission.code in ('attendance.check-in', 'notifications.read'))
     or (role.code = 'teacher' and permission.code in (
             'attendance.manage', 'fitness.manage', 'graduation.manage', 'lessons.manage', 'notifications.read', 'notifications.manage', 'ranking.manage'
   ))
   or (role.code = 'reception' and permission.code in (
             'attendance.manage', 'branches.manage', 'clients.manage', 'lessons.manage', 'notifications.read', 'notifications.manage'
   ))
   or (role.code = 'financial' and permission.code in ('billing.manage', 'reports.read'))
on conflict (role_id, permission_id) do nothing;

commit;