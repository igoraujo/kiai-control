begin;

alter table user_accounts
    add column if not exists is_platform_owner boolean not null default false,
    add column if not exists must_change_password boolean not null default false;

commit;
