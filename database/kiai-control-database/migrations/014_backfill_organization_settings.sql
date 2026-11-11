-- Legacy seeds created academies without the settings required by the student agenda.
-- Preserve configured policies and use the schema defaults for missing records.
insert into organization_settings (id, organization_id)
select gen_random_uuid(), id
from organizations
on conflict (organization_id) do nothing;
