-- Bootstrap de autenticacao: tabela de reset de senha e seed de admin inicial.
-- Senha padrao do admin: Admin@123456

create table if not exists password_reset_tokens (
    id uuid primary key,
    user_account_id uuid not null references user_accounts(id),
    token_hash varchar(255) not null,
    expires_at timestamptz not null,
    used_at timestamptz null,
    revoked_at timestamptz null,
    created_at timestamptz not null,
    created_by_ip varchar(80) null
);

create unique index if not exists ux_password_reset_tokens_token_hash
    on password_reset_tokens(token_hash);

create index if not exists ix_password_reset_tokens_user_account_id
    on password_reset_tokens(user_account_id);

create index if not exists ix_password_reset_tokens_expires_at
    on password_reset_tokens(expires_at);

insert into organizations (
    id,
    name,
    email,
    status,
    created_at
)
values (
    '11111111-1111-1111-1111-111111111111',
    'Kiai Control Academy',
    'admin@kiai.local',
    'active',
    now()
)
on conflict (id) do update
set
    name = excluded.name,
    email = excluded.email,
    status = excluded.status;

insert into persons (
    id,
    organization_id,
    full_name,
    email,
    status,
    hide_from_ranking,
    created_at
)
values (
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'Admin Kiai',
    'admin@kiai.local',
    'active',
    false,
    now()
)
on conflict (id) do update
set
    full_name = excluded.full_name,
    email = excluded.email,
    status = excluded.status,
    hide_from_ranking = excluded.hide_from_ranking,
    updated_at = now();

insert into user_accounts (
    id,
    person_id,
    email,
    password_hash,
    is_active,
    created_at
)
values (
    '33333333-3333-3333-3333-333333333333',
    '22222222-2222-2222-2222-222222222222',
    'admin@kiai.local',
    'PBKDF2$100000$AQIDBAUGBwgJCgsMDQ4PEA==$qcFegJie06o8c1nvLR19oaltyyqxYCeEEOBZYppGVW8=',
    true,
    now()
)
on conflict (id) do update
set
    email = excluded.email,
    password_hash = excluded.password_hash,
    is_active = excluded.is_active,
    updated_at = now();
