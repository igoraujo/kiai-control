begin;

alter table organizations add column if not exists privacy_hold boolean not null default false;
alter table audit_logs add column if not exists personal_data_redacted_at timestamptz;
create index if not exists ix_organizations_deleted_at on organizations (deleted_at);

create table if not exists platform_request_metrics (
    id uuid primary key default gen_random_uuid(),
    bucket_start timestamptz not null,
    organization_id uuid references organizations(id),
    route varchar(300) not null,
    method varchar(16) not null,
    status_code integer not null check (status_code between 100 and 599),
    requests bigint not null check (requests > 0),
    duration_total_ms double precision not null check (duration_total_ms >= 0),
    max_duration_ms double precision not null check (max_duration_ms >= 0),
    unique nulls not distinct (bucket_start, organization_id, route, method, status_code)
);

create index if not exists ix_platform_metrics_org_bucket on platform_request_metrics (organization_id, bucket_start);
create index if not exists ix_platform_metrics_bucket on platform_request_metrics (bucket_start);

commit;
