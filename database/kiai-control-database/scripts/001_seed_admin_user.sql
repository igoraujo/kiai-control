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

do $$
declare
    v_organization_id uuid;
    v_person_id uuid;
    v_user_account_id uuid;
begin
    select id
    into v_organization_id
    from organizations
    where email = 'admin@kiai.local'
    order by created_at
    limit 1;

        if v_organization_id is null then
        insert into organizations (id, name, email, status, created_at)
        values (gen_random_uuid(), 'Kiai Control Academy', 'admin@kiai.local', 'active', now())
                returning id into v_organization_id;
    end if;

        select p.id
        into v_person_id
        from persons p
        where p.organization_id = v_organization_id
            and p.email = 'admin@kiai.local'
            and p.deleted_at is null
    limit 1;

        if v_person_id is null then
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
            gen_random_uuid(),
            v_organization_id,
            'Admin Kiai',
            'admin@kiai.local',
            'active',
            false,
            now()
        )
        returning id into v_person_id;
    end if;

    select id
    into v_user_account_id
    from user_accounts
    where email = 'admin@kiai.local'
    limit 1;

    if v_user_account_id is null then
        insert into user_accounts (
            id,
            person_id,
            email,
            password_hash,
            is_active,
            created_at
        )
        values (
            gen_random_uuid(),
            v_person_id,
            'admin@kiai.local',
            'PBKDF2$100000$AQIDBAUGBwgJCgsMDQ4PEA==$qcFegJie06o8c1nvLR19oaltyyqxYCeEEOBZYppGVW8=',
            true,
            now()
        )
        returning id into v_user_account_id;
    end if;

    insert into person_roles (person_id, role_id, granted_at)
    select v_person_id, roles.id, now()
    from roles
    where roles.code = 'owner'
    on conflict (person_id, role_id) do nothing;
end;
$$;
