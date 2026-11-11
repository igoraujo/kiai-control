namespace KiaiControl.Repositories.Queries.Catalog;

public static class CatalogQueries
{
    public const string Lessons = @"
        select
            lessons.id,
            lessons.organization_id as OrganizationId,
            lessons.modality_id as ModalityId,
            lessons.modality_schedule_id as ModalityScheduleId,
            lessons.starts_at as StartsAt,
            lessons.ends_at as EndsAt,
            lessons.status::text as Status,
            lessons.cancellation_reason as CancellationReason,
            lessons.created_at as CreatedAt,
            lessons.updated_at as UpdatedAt,
            modality.name as ModalityName,
            branch.name as BranchName
        from lessons
        left join modalities modality on modality.id = lessons.modality_id
            and modality.organization_id = lessons.organization_id
        left join modality_schedules modality_schedule on modality_schedule.id = lessons.modality_schedule_id
            and modality_schedule.organization_id = lessons.organization_id
        left join branches branch on branch.id = modality_schedule.branch_id
            and branch.organization_id = lessons.organization_id
        where lessons.organization_id = @OrganizationId
            and (@TeacherId is null
                or modality_schedule.teacher_id = @TeacherId
                or exists (
                    select 1
                    from lesson_instructors instructor
                    where instructor.lesson_id = lessons.id
                        and instructor.teacher_id = @TeacherId
                ))
            and (@From is null
                or lessons.starts_at >= @From)
            and (@To is null
                or lessons.starts_at < @To)
            and (@Status is null
                or lessons.status::text = @Status)
            and (@Area = 'shared'
                or modality.type = 'hybrid'
                or (@Area = 'fight'
                    and modality.type = 'martial_art')
                or (@Area = 'gym'
                    and modality.type = 'fitness'))
        order by
            lessons.starts_at desc,
            lessons.id
        offset @Offset
        limit @PageSize;

        select count(1)
        from (
            select
                lessons.id,
                lessons.organization_id as OrganizationId,
                lessons.modality_id as ModalityId,
                lessons.modality_schedule_id as ModalityScheduleId,
                lessons.starts_at as StartsAt,
                lessons.ends_at as EndsAt,
                lessons.status::text as Status,
                lessons.cancellation_reason as CancellationReason,
                lessons.created_at as CreatedAt,
                lessons.updated_at as UpdatedAt,
                modality.name as ModalityName,
                branch.name as BranchName
            from lessons
            left join modalities modality on modality.id = lessons.modality_id
                and modality.organization_id = lessons.organization_id
            left join modality_schedules modality_schedule on modality_schedule.id = lessons.modality_schedule_id
                and modality_schedule.organization_id = lessons.organization_id
            left join branches branch on branch.id = modality_schedule.branch_id
                and branch.organization_id = lessons.organization_id
            where lessons.organization_id = @OrganizationId
                and (@TeacherId is null
                    or modality_schedule.teacher_id = @TeacherId
                    or exists (
                        select 1
                        from lesson_instructors instructor
                        where instructor.lesson_id = lessons.id
                            and instructor.teacher_id = @TeacherId
                    ))
                and (@From is null
                    or lessons.starts_at >= @From)
                and (@To is null
                    or lessons.starts_at < @To)
                and (@Status is null
                    or lessons.status::text = @Status)
                and (@Area = 'shared'
                    or modality.type = 'hybrid'
                    or (@Area = 'fight'
                        and modality.type = 'martial_art')
                    or (@Area = 'gym'
                        and modality.type = 'fitness'))
        ) result;
        ";

    public const string Clients = @"
        select
            c.id,
            c.organization_id as OrganizationId,
            c.person_id as PersonId,
            p.branch_id as BranchId,
            p.full_name as FullName,
            p.preferred_name as PreferredName,
            p.email,
            p.phone,
            p.birth_date as BirthDate,
            p.document,
            p.medical_notes as MedicalNotes,
            p.emergency_contact_name as EmergencyContactName,
            p.emergency_contact_phone as EmergencyContactPhone,
            p.hide_from_ranking as HideFromRanking,
            c.registration_code as RegistrationCode,
            c.joined_at as JoinedAt,
            c.current_status::text as Status,
            c.created_at as CreatedAt,
            c.updated_at as UpdatedAt,
            array_remove(
                array[
                    case
                        when exists (
                            select 1
                            from person_modalities assignment
                            join modalities area_modality on area_modality.id = assignment.modality_id
                                and area_modality.organization_id = c.organization_id
                            where assignment.person_id = c.person_id
                                and area_modality.type in ('martial_art', 'hybrid')
                        ) then 'fight'
                    end,
                    case
                        when exists (
                            select 1
                            from person_modalities assignment
                            join modalities area_modality on area_modality.id = assignment.modality_id
                                and area_modality.organization_id = c.organization_id
                            where assignment.person_id = c.person_id
                                and area_modality.type in ('fitness', 'hybrid')
                        ) then 'gym'
                    end
                ],
                null
            ) as Areas
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Area = 'shared'
                or exists (
                    select 1
                    from clients area_client
                    join person_modalities assignment on assignment.person_id = area_client.person_id
                    join modalities area_modality on area_modality.id = assignment.modality_id
                        and area_modality.organization_id = area_client.organization_id
                    where area_client.id = c.id
                        and area_client.organization_id = c.organization_id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                ))
            and (@Search is null
                or concat_ws(' ', p.full_name, p.preferred_name, p.email, p.phone, p.document, c.registration_code) ilike @Search)
        order by
            c.created_at desc,
            c.id
        offset @Offset rows fetch next @PageSize rows only;

        select count(1)
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Area = 'shared'
                or exists (
                    select 1
                    from clients area_client
                    join person_modalities assignment on assignment.person_id = area_client.person_id
                    join modalities area_modality on area_modality.id = assignment.modality_id
                        and area_modality.organization_id = area_client.organization_id
                    where area_client.id = c.id
                        and area_client.organization_id = c.organization_id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                ))
            and (@Search is null
                or concat_ws(' ', p.full_name, p.preferred_name, p.email, p.phone, p.document, c.registration_code) ilike @Search);
        ";

    public const string Teachers = @"
        select
            t.id,
            t.organization_id as OrganizationId,
            t.person_id as PersonId,
            p.branch_id as BranchId,
            p.full_name as FullName,
            p.preferred_name as PreferredName,
            p.email,
            p.phone,
            p.birth_date as BirthDate,
            p.document,
            p.medical_notes as MedicalNotes,
            p.emergency_contact_name as EmergencyContactName,
            p.emergency_contact_phone as EmergencyContactPhone,
            p.hide_from_ranking as HideFromRanking,
            t.hired_at as HiredAt,
            t.active_from as ActiveFrom,
            t.current_status::text as Status,
            t.created_at as CreatedAt,
            t.updated_at as UpdatedAt
        from teachers t
        join persons p on p.id = t.person_id
        where t.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Area = 'shared'
                or exists (
                    select 1
                    from modality_schedules schedule
                    join modalities area_modality on area_modality.id = schedule.modality_id
                        and area_modality.organization_id = t.organization_id
                    where schedule.teacher_id = t.id
                        and schedule.organization_id = t.organization_id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                )
                or exists (
                    select 1
                    from lesson_instructors instructor
                    join lessons lesson on lesson.id = instructor.lesson_id
                        and lesson.organization_id = t.organization_id
                    join modalities area_modality on area_modality.id = lesson.modality_id
                        and area_modality.organization_id = t.organization_id
                    where instructor.teacher_id = t.id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                ))
            and (@Search is null
                or concat_ws(' ', p.full_name, p.preferred_name, p.email, p.phone, p.document) ilike @Search)
        order by
            t.created_at desc,
            t.id
        offset @Offset rows fetch next @PageSize rows only;

        select count(1)
        from teachers t
        join persons p on p.id = t.person_id
        where t.organization_id = @OrganizationId
            and p.deleted_at is null
            and (@Area = 'shared'
                or exists (
                    select 1
                    from modality_schedules schedule
                    join modalities area_modality on area_modality.id = schedule.modality_id
                        and area_modality.organization_id = t.organization_id
                    where schedule.teacher_id = t.id
                        and schedule.organization_id = t.organization_id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                )
                or exists (
                    select 1
                    from lesson_instructors instructor
                    join lessons lesson on lesson.id = instructor.lesson_id
                        and lesson.organization_id = t.organization_id
                    join modalities area_modality on area_modality.id = lesson.modality_id
                        and area_modality.organization_id = t.organization_id
                    where instructor.teacher_id = t.id
                        and (@Area = 'shared'
                            or area_modality.type = 'hybrid'
                            or (@Area = 'fight'
                                and area_modality.type = 'martial_art')
                            or (@Area = 'gym'
                                and area_modality.type = 'fitness'))
                ))
            and (@Search is null
                or concat_ws(' ', p.full_name, p.preferred_name, p.email, p.phone, p.document) ilike @Search);
        ";

    public const string Branches = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            address_line as AddressLine,
            city,
            state,
            postal_code as PostalCode,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from branches
        where organization_id = @OrganizationId
            and (@Search is null
                or concat_ws(' ', name, city, state, address_line) ilike @Search)
        order by name
        offset @Offset rows fetch next @PageSize rows only;

        select count(1)
        from branches
        where organization_id = @OrganizationId
            and (@Search is null
                or concat_ws(' ', name, city, state, address_line) ilike @Search);
        ";

    public const string Modalities = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            type::text as Type,
            description,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from modalities
        where organization_id = @OrganizationId
            and (@Area = 'shared'
                or modalities.type = 'hybrid'
                or (@Area = 'fight'
                    and modalities.type = 'martial_art')
                or (@Area = 'gym'
                    and modalities.type = 'fitness'))
            and (@Search is null
                or concat_ws(' ', name, description, type::text) ilike @Search)
        order by name
        offset @Offset rows fetch next @PageSize rows only;

        select count(1)
        from modalities
        where organization_id = @OrganizationId
            and (@Area = 'shared'
                or modalities.type = 'hybrid'
                or (@Area = 'fight'
                    and modalities.type = 'martial_art')
                or (@Area = 'gym'
                    and modalities.type = 'fitness'))
            and (@Search is null
                or concat_ws(' ', name, description, type::text) ilike @Search);
        ";
}
