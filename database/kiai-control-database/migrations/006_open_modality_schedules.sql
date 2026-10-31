create table modality_schedules (
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

create index ix_modality_schedules_org_modality_active
    on modality_schedules (organization_id, modality_id, is_active);

alter table lessons alter column class_group_id drop not null;
alter table lessons add column modality_id uuid references modalities(id);
alter table lessons add column modality_schedule_id uuid references modality_schedules(id);
alter table lessons add constraint ck_lessons_schedule_context check (
    (class_group_id is not null and modality_id is null and modality_schedule_id is null)
    or (class_group_id is null and modality_id is not null and modality_schedule_id is not null)
);

create index ix_lessons_org_modality_starts on lessons (organization_id, modality_id, starts_at);
create unique index uq_lessons_org_modality_schedule_starts
    on lessons (organization_id, modality_schedule_id, starts_at);
