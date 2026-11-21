begin;

-- Personal information is always resolved from the authenticated person and tenant.
alter table persons add column profile_photo bytea;
alter table persons add column profile_photo_content_type varchar(30);
alter table persons add constraint ck_profile_photo_size check (octet_length(profile_photo) <= 524288);
alter table persons add constraint ck_profile_photo_type check (
    (profile_photo is null and profile_photo_content_type is null)
    or (profile_photo is not null and profile_photo_content_type is not null and profile_photo_content_type in ('image/jpeg', 'image/png'))
);

create table mobile_preferences (
    organization_id uuid not null references organizations(id),
    person_id uuid not null references persons(id),
    push_enabled boolean not null default false,
    primary key (organization_id, person_id)
);

create table mobile_devices (
    installation_id varchar(200) primary key,
    organization_id uuid not null references organizations(id),
    person_id uuid not null references persons(id),
    token varchar(4096) not null,
    session_version integer not null,
    platform varchar(10) not null check (platform in ('android', 'ios')),
    updated_at timestamptz not null default now()
);
create index ix_mobile_devices_person on mobile_devices (organization_id, person_id);

-- Durable delivery per device, separate from whether the notification has been read.
create table push_deliveries (
    notification_id uuid not null references notifications(id) on delete cascade,
    installation_id varchar(200) not null references mobile_devices(installation_id) on delete cascade,
    attempts integer not null default 0,
    available_at timestamptz not null default now(),
    accepted_at timestamptz,
    primary key (notification_id, installation_id)
);
create index ix_push_deliveries_due on push_deliveries (available_at) where accepted_at is null;

-- Self check-ins now always require validation by authorized staff.
update organization_settings set require_teacher_confirmation = true;

commit;
