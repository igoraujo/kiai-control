begin;

create table graduation_belts (
    id uuid primary key,
    graduation_system_id uuid not null references graduation_systems(id),
    name varchar(80) not null,
    hierarchy_order integer not null check (hierarchy_order > 0),
    degree_count integer not null check (degree_count between 0 and 99),
    constraint uq_graduation_belts_system_order unique (graduation_system_id, hierarchy_order),
    constraint uq_graduation_belts_system_name unique (graduation_system_id, name),
    constraint uq_graduation_belts_id_system unique (id, graduation_system_id)
);

alter table graduation_levels
    add column belt_id uuid,
    add column degree_number integer not null default 0 check (degree_number between 0 and 99);

insert into graduation_belts (id, graduation_system_id, name, hierarchy_order, degree_count)
select gen_random_uuid(), level.graduation_system_id, level.name, level.hierarchy_order, 0
from graduation_levels level;

update graduation_levels level
set belt_id = belt.id,
    hierarchy_order = belt.hierarchy_order * 100
from graduation_belts belt
where belt.graduation_system_id = level.graduation_system_id
  and belt.name = level.name;

alter table graduation_levels
    alter column belt_id set not null,
    add constraint fk_graduation_levels_belt_system
        foreign key (belt_id, graduation_system_id)
        references graduation_belts(id, graduation_system_id),
    add constraint uq_graduation_levels_belt_degree unique (belt_id, degree_number);

commit;
