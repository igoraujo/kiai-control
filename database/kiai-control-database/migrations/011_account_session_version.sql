begin;
alter table user_accounts add column if not exists session_version integer not null default 0;
create index if not exists ix_persons_org_name_search on persons (organization_id, lower(full_name));
commit;
