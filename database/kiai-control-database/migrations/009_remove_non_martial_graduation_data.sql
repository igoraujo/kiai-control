begin;

delete from graduation_events event
where event.modality_id in (
    select modality.id
    from modalities modality
    where modality.type <> 'martial_art'
)
or exists (
    select 1
    from graduation_levels level
    join graduation_systems system on system.id = level.graduation_system_id
    join modalities modality on modality.id = system.modality_id
    where modality.type <> 'martial_art'
      and level.id in (event.from_level_id, event.to_level_id)
);

delete from client_graduations graduation
where graduation.modality_id in (
    select modality.id
    from modalities modality
    where modality.type <> 'martial_art'
)
or exists (
    select 1
    from graduation_levels level
    join graduation_systems system on system.id = level.graduation_system_id
    join modalities modality on modality.id = system.modality_id
    where modality.type <> 'martial_art'
      and level.id = graduation.current_level_id
);

delete from graduation_levels level
using graduation_systems system, modalities modality
where level.graduation_system_id = system.id
  and system.modality_id = modality.id
  and modality.type <> 'martial_art';

delete from graduation_belts belt
using graduation_systems system, modalities modality
where belt.graduation_system_id = system.id
  and system.modality_id = modality.id
  and modality.type <> 'martial_art';

delete from graduation_systems system
using modalities modality
where system.modality_id = modality.id
  and modality.type <> 'martial_art';

commit;
