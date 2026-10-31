begin;

alter table lessons drop constraint if exists ck_lessons_schedule_context;

update lessons lesson
set modality_id = modality.id
from class_groups class_group
join modalities modality
  on modality.id = class_group.modality_id
 and modality.organization_id = class_group.organization_id
where lesson.class_group_id = class_group.id
  and lesson.organization_id = class_group.organization_id
  and lesson.modality_id is null;

update ranking_cycles cycle
set modality_id = modality.id
from class_groups class_group
join modalities modality
  on modality.id = class_group.modality_id
 and modality.organization_id = class_group.organization_id
where cycle.class_group_id = class_group.id
  and cycle.organization_id = class_group.organization_id
  and cycle.modality_id is null;

insert into person_modalities (person_id, modality_id, linked_at)
select person.id, modality.id, min(enrollment.enrolled_at)
from enrollments enrollment
join class_groups class_group
  on class_group.id = enrollment.class_group_id
 and class_group.organization_id = enrollment.organization_id
join modalities modality
  on modality.id = class_group.modality_id
 and modality.organization_id = class_group.organization_id
join clients client
  on client.id = enrollment.client_id
 and client.organization_id = enrollment.organization_id
join persons person
  on person.id = client.person_id
 and person.organization_id = enrollment.organization_id
 and person.deleted_at is null
where enrollment.status = 'active'
group by person.id, modality.id
on conflict (person_id, modality_id) do nothing;

alter table lessons alter column modality_id set not null;
alter table lessons drop column if exists class_group_id;
alter table ranking_cycles drop column if exists class_group_id;

drop table if exists enrollments;
drop table if exists class_schedules;
drop table if exists class_group_teachers;
drop table if exists class_groups;

delete from organization_role_permissions
where permission_id in (
    select id from permissions where code in ('class-groups.read', 'class-groups.manage')
);

delete from role_permissions
where permission_id in (
    select id from permissions where code in ('class-groups.read', 'class-groups.manage')
);

delete from permissions
where code in ('class-groups.read', 'class-groups.manage');

drop type if exists enrollment_status;
drop type if exists class_group_status;

commit;
