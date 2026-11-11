begin;

create table if not exists platform_incidents (
    id uuid primary key,
    organization_id uuid references organizations(id),
    actor_user_id uuid references user_accounts(id),
    category varchar(40) not null check (category in ('request_failure', 'authentication_failure', 'audit_failure')),
    method varchar(16) not null,
    route varchar(300) not null,
    status_code integer not null check (status_code between 100 and 599),
    correlation_id varchar(120) not null,
    created_at timestamptz not null default now()
);

create index if not exists ix_platform_incidents_created on platform_incidents (created_at desc, id desc);
create index if not exists ix_platform_incidents_org_created on platform_incidents (organization_id, created_at desc);
create index if not exists ix_platform_incidents_category_created on platform_incidents (category, created_at desc);
create index if not exists ix_audit_logs_created on audit_logs (created_at desc, id desc);
create index if not exists ix_audit_logs_actor_user_created on audit_logs (actor_user_account_id, created_at desc);

commit;
