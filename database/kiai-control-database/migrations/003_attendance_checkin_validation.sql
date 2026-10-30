begin;

alter type attendance_status add value if not exists 'pending_confirmation';

alter table attendances
    add column if not exists validated_at timestamptz,
    add column if not exists validated_by_person_id uuid references persons(id);

commit;