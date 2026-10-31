begin;

create table if not exists organization_role_permissions (
    organization_id uuid not null references organizations(id) on delete cascade,
    role_id uuid not null references roles(id) on delete cascade,
    permission_id uuid not null references permissions(id) on delete cascade,
    is_granted boolean not null,
    changed_at timestamptz not null default now(),
    changed_by_user_account_id uuid references user_accounts(id),
    primary key (organization_id, role_id, permission_id)
);

create index if not exists ix_organization_role_permissions_role
    on organization_role_permissions (organization_id, role_id);

commit;