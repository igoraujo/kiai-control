-- Bootstrap idempotente para um banco PostgreSQL novo.
-- Pode ser executado novamente no mesmo banco sem recriar objetos existentes.
-- Para bancos já existentes, continue usando o fluxo de migrations.

begin;

create extension if not exists pgcrypto;

do $$ begin create type organization_status as enum ('active', 'inactive', 'suspended');
exception when duplicate_object then null; end $$;
do $$ begin create type person_status as enum ('active', 'inactive', 'suspended', 'canceled', 'trial');
exception when duplicate_object then null; end $$;
do $$ begin create type role_code as enum ('client', 'teacher', 'admin', 'financial', 'reception', 'owner');
exception when duplicate_object then null; end $$;
do $$ begin create type modality_type as enum ('martial_art', 'fitness', 'hybrid');
exception when duplicate_object then null; end $$;
do $$ begin create type lesson_status as enum ('scheduled', 'completed', 'canceled', 'rescheduled');
exception when duplicate_object then null; end $$;
do $$ begin create type attendance_status as enum ('present', 'absent', 'justified_absence', 'late', 'pending_confirmation');
exception when duplicate_object then null; end $$;
do $$ begin create type invoice_status as enum ('pending', 'paid', 'overdue', 'canceled', 'exempt', 'partially_paid');
exception when duplicate_object then null; end $$;
do $$ begin create type payment_method as enum ('cash', 'pix', 'bank_transfer', 'credit_card', 'debit_card', 'boleto', 'manual_adjustment');
exception when duplicate_object then null; end $$;
do $$ begin create type graduation_event_type as enum ('promotion', 'demotion', 'correction');
exception when duplicate_object then null; end $$;
do $$ begin create type goal_type as enum ('training_days', 'attendance_rate', 'load', 'body_measurement', 'weight', 'conditioning', 'score', 'category', 'custom');
exception when duplicate_object then null; end $$;
do $$ begin create type goal_status as enum ('active', 'achieved', 'canceled', 'expired');
exception when duplicate_object then null; end $$;
do $$ begin create type ranking_cycle_status as enum ('open', 'closed', 'archived');
exception when duplicate_object then null; end $$;
do $$ begin create type notification_channel as enum ('in_app', 'push', 'email');
exception when duplicate_object then null; end $$;
do $$ begin create type notification_status as enum ('pending', 'sent', 'read', 'failed');
exception when duplicate_object then null; end $$;

create table if not exists organizations (
    id uuid primary key,
    name varchar(160) not null,
    trade_name varchar(160),
    document varchar(30),
    email varchar(160),
    phone varchar(30),
    status organization_status not null default 'active',
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    deleted_at timestamptz
);
create unique index if not exists uq_organizations_document on organizations (document) where document is not null;
create index if not exists ix_organizations_email on organizations (email);
create index if not exists ix_organizations_status on organizations (status);

create table if not exists organization_settings (
    id uuid primary key,
    organization_id uuid not null unique references organizations(id),
    timezone varchar(60) not null default 'America/Sao_Paulo',
    currency_code char(3) not null default 'BRL',
    allow_client_checkin boolean not null default false,
    require_teacher_confirmation boolean not null default true,
    default_late_tolerance_minutes integer not null default 10 check (default_late_tolerance_minutes >= 0),
    created_at timestamptz not null default now(),
    updated_at timestamptz
);

create table if not exists branches (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    name varchar(160) not null,
    address_line varchar(255),
    city varchar(100),
    state varchar(50),
    postal_code varchar(20),
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_branches_org_name unique (organization_id, name)
);
create index if not exists ix_branches_organization_id on branches (organization_id);
create index if not exists ix_branches_city on branches (city);

create table if not exists persons (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    branch_id uuid references branches(id),
    full_name varchar(180) not null,
    preferred_name varchar(100),
    email varchar(160),
    phone varchar(30),
    birth_date date,
    document varchar(30),
    status person_status not null default 'active',
    medical_notes text,
    emergency_contact_name varchar(160),
    emergency_contact_phone varchar(30),
    hide_from_ranking boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    deleted_at timestamptz
);
create unique index if not exists uq_persons_org_email on persons (organization_id, email)
    where email is not null and deleted_at is null;
create unique index if not exists uq_persons_org_document on persons (organization_id, document)
    where document is not null and deleted_at is null;
create index if not exists ix_persons_org_full_name on persons (organization_id, full_name);
create index if not exists ix_persons_org_status on persons (organization_id, status);
create index if not exists ix_persons_branch_id on persons (branch_id);

create table if not exists user_accounts (
    id uuid primary key,
    person_id uuid not null unique references persons(id),
    email varchar(160) not null unique,
    password_hash varchar(255) not null,
    is_active boolean not null default true,
    is_platform_owner boolean not null default false,
    must_change_password boolean not null default false,
    last_login_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz
);
create index if not exists ix_user_accounts_is_active on user_accounts (is_active);

create table if not exists refresh_tokens (
    id uuid primary key,
    user_account_id uuid not null references user_accounts(id),
    token_hash varchar(255) not null unique,
    expires_at timestamptz not null,
    revoked_at timestamptz,
    replaced_by_token_id uuid references refresh_tokens(id),
    created_at timestamptz not null default now(),
    created_by_ip varchar(80),
    revoked_by_ip varchar(80)
);
create index if not exists ix_refresh_tokens_user_account_id on refresh_tokens (user_account_id);
create index if not exists ix_refresh_tokens_expires_at on refresh_tokens (expires_at);
create index if not exists ix_refresh_tokens_revoked_at on refresh_tokens (revoked_at);

create table if not exists password_reset_tokens (
    id uuid primary key,
    user_account_id uuid not null references user_accounts(id),
    token_hash varchar(255) not null unique,
    expires_at timestamptz not null,
    used_at timestamptz,
    revoked_at timestamptz,
    created_at timestamptz not null default now(),
    created_by_ip varchar(80)
);
create index if not exists ix_password_reset_tokens_user_account_id on password_reset_tokens (user_account_id);
create index if not exists ix_password_reset_tokens_expires_at on password_reset_tokens (expires_at);

create table if not exists roles (
    id uuid primary key,
    code role_code not null unique,
    name varchar(80) not null unique,
    description varchar(255)
);

create table if not exists permissions (
    id uuid primary key,
    code varchar(120) not null unique,
    name varchar(120) not null unique,
    description varchar(255)
);

create table if not exists role_permissions (
    role_id uuid not null references roles(id),
    permission_id uuid not null references permissions(id),
    granted_at timestamptz not null default now(),
    primary key (role_id, permission_id)
);
create index if not exists ix_role_permissions_permission_id on role_permissions (permission_id);

create table if not exists person_roles (
    person_id uuid not null references persons(id),
    role_id uuid not null references roles(id),
    granted_by_user_account_id uuid references user_accounts(id),
    granted_at timestamptz not null default now(),
    primary key (person_id, role_id)
);
create index if not exists ix_person_roles_role_id on person_roles (role_id);

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

create table if not exists clients (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    person_id uuid not null unique references persons(id),
    registration_code varchar(40),
    joined_at date,
    current_status person_status not null default 'active',
    created_at timestamptz not null default now(),
    updated_at timestamptz
);
create unique index if not exists uq_clients_org_registration on clients (organization_id, registration_code)
    where registration_code is not null;
create index if not exists ix_clients_org_status on clients (organization_id, current_status);

create table if not exists teachers (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    person_id uuid not null unique references persons(id),
    hired_at date,
    active_from date,
    current_status person_status not null default 'active',
    created_at timestamptz not null default now(),
    updated_at timestamptz
);
create index if not exists ix_teachers_org_status on teachers (organization_id, current_status);

create table if not exists modalities (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    name varchar(120) not null,
    type modality_type not null,
    description text,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_modalities_org_name unique (organization_id, name)
);
create index if not exists ix_modalities_org_type on modalities (organization_id, type);
create index if not exists ix_modalities_is_active on modalities (is_active);

create table if not exists person_modalities (
    person_id uuid not null references persons(id),
    modality_id uuid not null references modalities(id),
    linked_at timestamptz not null default now(),
    primary key (person_id, modality_id)
);
create index if not exists ix_person_modalities_modality_id on person_modalities (modality_id);

create table if not exists modality_schedules (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    modality_id uuid not null references modalities(id),
    branch_id uuid references branches(id),
    teacher_id uuid references teachers(id),
    day_of_week integer not null check (day_of_week between 0 and 6),
    starts_at time not null,
    ends_at time not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint ck_modality_schedules_valid_interval check (ends_at > starts_at),
    constraint uq_modality_schedules_slot unique nulls not distinct
        (organization_id, modality_id, branch_id, day_of_week, starts_at)
);
create index if not exists ix_modality_schedules_org_modality_active
    on modality_schedules (organization_id, modality_id, is_active);

create table if not exists lessons (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    modality_id uuid not null references modalities(id),
    modality_schedule_id uuid references modality_schedules(id),
    starts_at timestamptz not null,
    ends_at timestamptz not null,
    status lesson_status not null default 'scheduled',
    cancellation_reason varchar(255),
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint ck_lessons_valid_interval check (ends_at > starts_at)
);
create index if not exists ix_lessons_org_status on lessons (organization_id, status);
create index if not exists ix_lessons_org_modality_starts on lessons (organization_id, modality_id, starts_at);
create unique index if not exists uq_lessons_org_modality_schedule_starts
    on lessons (organization_id, modality_schedule_id, starts_at);

create table if not exists lesson_instructors (
    id uuid primary key,
    lesson_id uuid not null references lessons(id),
    teacher_id uuid not null references teachers(id),
    is_substitute boolean not null default false,
    created_at timestamptz not null default now(),
    constraint uq_lesson_instructors_lesson_teacher unique (lesson_id, teacher_id)
);
create index if not exists ix_lesson_instructors_teacher_id on lesson_instructors (teacher_id);

create table if not exists attendances (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    lesson_id uuid not null references lessons(id),
    client_id uuid not null references clients(id),
    status attendance_status not null,
    checked_in_at timestamptz,
    checked_by_teacher_id uuid references teachers(id),
    validated_at timestamptz,
    validated_by_person_id uuid references persons(id),
    notes varchar(255),
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_attendances_org_lesson_client unique (organization_id, lesson_id, client_id)
);
create index if not exists ix_attendances_org_status on attendances (organization_id, status);
create index if not exists ix_attendances_client_id on attendances (client_id);
create index if not exists ix_attendances_checked_by_teacher_id on attendances (checked_by_teacher_id);

create table if not exists plans (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    name varchar(120) not null,
    description varchar(255),
    monthly_amount numeric(12,2) not null check (monthly_amount >= 0),
    due_day integer not null check (due_day between 1 and 28),
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_plans_org_name unique (organization_id, name)
);
create index if not exists ix_plans_org_active on plans (organization_id, is_active);

create table if not exists client_plan_subscriptions (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    client_id uuid not null references clients(id),
    plan_id uuid not null references plans(id),
    starts_at date not null,
    ends_at date,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint ck_client_plan_subscriptions_valid_period check (ends_at is null or ends_at >= starts_at)
);
create index if not exists ix_client_plan_subscriptions_org_client_active
    on client_plan_subscriptions (organization_id, client_id, is_active);
create unique index if not exists uq_client_plan_subscriptions_one_active
    on client_plan_subscriptions (organization_id, client_id) where is_active = true;

create table if not exists invoices (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    client_plan_subscription_id uuid not null references client_plan_subscriptions(id),
    client_id uuid not null references clients(id),
    due_date date not null,
    amount numeric(12,2) not null check (amount >= 0),
    discount_amount numeric(12,2) not null default 0 check (discount_amount >= 0),
    interest_amount numeric(12,2) not null default 0 check (interest_amount >= 0),
    fine_amount numeric(12,2) not null default 0 check (fine_amount >= 0),
    paid_amount numeric(12,2) not null default 0 check (paid_amount >= 0),
    status invoice_status not null default 'pending',
    issued_at timestamptz not null default now(),
    paid_at timestamptz,
    canceled_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz
);
create index if not exists ix_invoices_org_client_due on invoices (organization_id, client_id, due_date);
create index if not exists ix_invoices_org_status_due on invoices (organization_id, status, due_date);
create index if not exists ix_invoices_subscription on invoices (client_plan_subscription_id);
create unique index if not exists uq_invoices_subscription_due_date
    on invoices (client_plan_subscription_id, due_date);

create table if not exists payments (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    invoice_id uuid not null references invoices(id),
    amount numeric(12,2) not null check (amount > 0),
    method payment_method not null,
    paid_at timestamptz not null,
    external_reference varchar(120),
    notes varchar(255),
    created_at timestamptz not null default now()
);
create index if not exists ix_payments_org_invoice on payments (organization_id, invoice_id);
create index if not exists ix_payments_org_method on payments (organization_id, method);
create index if not exists ix_payments_paid_at on payments (paid_at);
create index if not exists ix_payments_external_reference on payments (external_reference);

create table if not exists graduation_systems (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    modality_id uuid not null references modalities(id),
    name varchar(120) not null,
    description text,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_graduation_systems_org_modality_name unique (organization_id, modality_id, name)
);
create index if not exists ix_graduation_systems_active on graduation_systems (is_active);

create table if not exists graduation_belts (
    id uuid primary key,
    graduation_system_id uuid not null references graduation_systems(id),
    name varchar(80) not null,
    hierarchy_order integer not null check (hierarchy_order > 0),
    degree_count integer not null check (degree_count between 0 and 99),
    constraint uq_graduation_belts_system_order unique (graduation_system_id, hierarchy_order),
    constraint uq_graduation_belts_system_name unique (graduation_system_id, name),
    constraint uq_graduation_belts_id_system unique (id, graduation_system_id)
);

create table if not exists graduation_levels (
    id uuid primary key,
    graduation_system_id uuid not null references graduation_systems(id),
    belt_id uuid not null,
    name varchar(80) not null,
    hierarchy_order integer not null,
    degree_number integer not null default 0 check (degree_number between 0 and 99),
    min_training_days integer not null default 0 check (min_training_days >= 0),
    min_attendance_percentage numeric(5,2) check (min_attendance_percentage between 0 and 100),
    min_lessons integer not null default 0 check (min_lessons >= 0),
    min_age integer check (min_age >= 0),
    requires_manual_evaluation boolean not null default false,
    created_at timestamptz not null default now(),
    constraint fk_graduation_levels_belt_system foreign key (belt_id, graduation_system_id)
        references graduation_belts(id, graduation_system_id),
    constraint uq_graduation_levels_system_order unique (graduation_system_id, hierarchy_order),
    constraint uq_graduation_levels_system_name unique (graduation_system_id, name),
    constraint uq_graduation_levels_belt_degree unique (belt_id, degree_number)
);

create table if not exists client_graduations (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    client_id uuid not null references clients(id),
    modality_id uuid not null references modalities(id),
    current_level_id uuid not null references graduation_levels(id),
    granted_at date not null,
    granted_by_teacher_id uuid references teachers(id),
    notes varchar(255),
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_client_graduations_org_client_modality unique (organization_id, client_id, modality_id)
);
create index if not exists ix_client_graduations_level on client_graduations (current_level_id);
create index if not exists ix_client_graduations_teacher on client_graduations (granted_by_teacher_id);

create table if not exists graduation_events (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    client_id uuid not null references clients(id),
    modality_id uuid not null references modalities(id),
    from_level_id uuid references graduation_levels(id),
    to_level_id uuid not null references graduation_levels(id),
    event_type graduation_event_type not null default 'promotion',
    event_date date not null,
    teacher_id uuid references teachers(id),
    justification varchar(255),
    created_at timestamptz not null default now()
);
create index if not exists ix_graduation_events_org_client_date
    on graduation_events (organization_id, client_id, modality_id, event_date);
create index if not exists ix_graduation_events_type on graduation_events (event_type);
create index if not exists ix_graduation_events_teacher on graduation_events (teacher_id);

create table if not exists fitness_goals (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    client_id uuid not null references clients(id),
    modality_id uuid references modalities(id),
    title varchar(160) not null,
    description text,
    goal_type goal_type not null,
    target_value numeric(12,2) not null check (target_value > 0),
    current_value numeric(12,2) not null default 0,
    unit varchar(30),
    starts_at date not null,
    due_date date not null,
    status goal_status not null default 'active',
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint ck_fitness_goals_valid_period check (due_date >= starts_at)
);
create index if not exists ix_fitness_goals_org_client_status on fitness_goals (organization_id, client_id, status);
create index if not exists ix_fitness_goals_org_type on fitness_goals (organization_id, goal_type);
create index if not exists ix_fitness_goals_due_date on fitness_goals (due_date);

create table if not exists fitness_progress_records (
    id uuid primary key,
    fitness_goal_id uuid not null references fitness_goals(id),
    recorded_value numeric(12,2) not null,
    notes varchar(255),
    recorded_at timestamptz not null default now(),
    created_by_teacher_id uuid references teachers(id)
);
create index if not exists ix_fitness_progress_goal_recorded on fitness_progress_records (fitness_goal_id, recorded_at);
create index if not exists ix_fitness_progress_teacher on fitness_progress_records (created_by_teacher_id);

create table if not exists ranking_cycles (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    modality_id uuid references modalities(id),
    name varchar(120) not null,
    starts_at date not null,
    ends_at date not null,
    status ranking_cycle_status not null default 'open',
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    constraint uq_ranking_cycles_org_name_start unique (organization_id, name, starts_at),
    constraint ck_ranking_cycles_valid_period check (ends_at >= starts_at)
);
create index if not exists ix_ranking_cycles_org_status on ranking_cycles (organization_id, status);
create index if not exists ix_ranking_cycles_modality on ranking_cycles (modality_id);

create table if not exists ranking_entries (
    id uuid primary key,
    ranking_cycle_id uuid not null references ranking_cycles(id),
    client_id uuid not null references clients(id),
    points integer not null default 0,
    attendance_count integer not null default 0,
    streak_count integer not null default 0,
    position integer,
    updated_at timestamptz not null default now(),
    constraint uq_ranking_entries_cycle_client unique (ranking_cycle_id, client_id),
    constraint ck_ranking_entries_nonnegative check (points >= 0 and attendance_count >= 0 and streak_count >= 0)
);
create index if not exists ix_ranking_entries_cycle_position on ranking_entries (ranking_cycle_id, position);
create index if not exists ix_ranking_entries_points on ranking_entries (points);

create table if not exists medals (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    code varchar(60) not null,
    name varchar(120) not null,
    description varchar(255),
    points_bonus integer not null default 0,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    constraint uq_medals_org_code unique (organization_id, code),
    constraint ck_medals_nonnegative_bonus check (points_bonus >= 0)
);
create index if not exists ix_medals_org_active on medals (organization_id, is_active);

create table if not exists person_medals (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    person_id uuid not null references persons(id),
    medal_id uuid not null references medals(id),
    awarded_at timestamptz not null default now(),
    reason varchar(255),
    constraint uq_person_medals_award unique (organization_id, person_id, medal_id, awarded_at)
);
create index if not exists ix_person_medals_medal on person_medals (medal_id);

create table if not exists notifications (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    person_id uuid not null references persons(id),
    title varchar(160) not null,
    body text not null,
    channel notification_channel not null default 'in_app',
    status notification_status not null default 'pending',
    scheduled_at timestamptz,
    sent_at timestamptz,
    read_at timestamptz,
    created_at timestamptz not null default now()
);
create index if not exists ix_notifications_org_person_status on notifications (organization_id, person_id, status);
create index if not exists ix_notifications_scheduled_at on notifications (scheduled_at);
create index if not exists ix_notifications_channel on notifications (channel);

create table if not exists audit_logs (
    id uuid primary key,
    organization_id uuid not null references organizations(id),
    actor_person_id uuid references persons(id),
    actor_user_account_id uuid references user_accounts(id),
    action varchar(120) not null,
    entity_name varchar(120) not null,
    entity_id uuid,
    metadata jsonb,
    ip_address varchar(80),
    user_agent varchar(255),
    created_at timestamptz not null default now()
);
create index if not exists ix_audit_logs_org_created on audit_logs (organization_id, created_at);
create index if not exists ix_audit_logs_org_entity on audit_logs (organization_id, entity_name, entity_id);
create index if not exists ix_audit_logs_actor_person on audit_logs (actor_person_id);
create index if not exists ix_audit_logs_action on audit_logs (action);

commit;
