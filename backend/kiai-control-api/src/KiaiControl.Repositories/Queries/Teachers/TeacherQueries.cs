namespace KiaiControl.Repositories.Queries.Teachers;

public static class TeacherQueries
{
    public const string Insert = @"
        with inserted_person as (
            insert into persons (
                id, organization_id, branch_id, full_name, preferred_name, email, phone, birth_date,
                document, status, medical_notes, emergency_contact_name, emergency_contact_phone,
                hide_from_ranking, created_at
            )
            values (
                @PersonId, @OrganizationId, @BranchId, @FullName, @PreferredName, @Email, @Phone,
                @BirthDate, @Document, @Status::person_status, @MedicalNotes, @EmergencyContactName,
                @EmergencyContactPhone, @HideFromRanking, @CreatedAt
            )
            returning id, branch_id, full_name, preferred_name, email, phone, birth_date, document,
                medical_notes, emergency_contact_name, emergency_contact_phone, hide_from_ranking
        ), inserted_teacher as (
            insert into teachers (id, organization_id, person_id, hired_at, active_from, current_status, created_at)
            select @Id, @OrganizationId, inserted_person.id, @HiredAt, @ActiveFrom, @Status::person_status, @CreatedAt
            from inserted_person
            returning id, organization_id, person_id, hired_at, active_from, current_status, created_at, updated_at
        ), assigned_role as (
            insert into person_roles (person_id, role_id)
            select inserted_person.id, roles.id
            from inserted_person
            join roles on roles.code = 'teacher'::role_code
            returning person_id
        )
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
        from inserted_teacher t
        join inserted_person p on p.id = t.person_id
        join assigned_role ar on ar.person_id = t.person_id;
        ";

    public const string ListByOrganization = @"
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
        where t.organization_id = @OrganizationId and p.deleted_at is null
        order by t.created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
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
        where t.organization_id = @OrganizationId and t.id = @Id and p.deleted_at is null;
        ";

    public const string Update = @"
        with target as (
            select t.id, t.organization_id, t.person_id
            from teachers t
            join persons p on p.id = t.person_id and p.deleted_at is null
            where t.organization_id = @OrganizationId and t.id = @Id
        ), updated_person as (
            update persons p
            set
                branch_id = @BranchId,
                full_name = @FullName,
                preferred_name = @PreferredName,
                email = @Email,
                phone = @Phone,
                birth_date = @BirthDate,
                document = @Document,
                status = @Status::person_status,
                medical_notes = @MedicalNotes,
                emergency_contact_name = @EmergencyContactName,
                emergency_contact_phone = @EmergencyContactPhone,
                hide_from_ranking = @HideFromRanking,
                updated_at = @UpdatedAt
            from target t
            where p.id = t.person_id
            returning p.id, p.branch_id, p.full_name, p.preferred_name, p.email, p.phone, p.birth_date,
                p.document, p.medical_notes, p.emergency_contact_name, p.emergency_contact_phone, p.hide_from_ranking
        ), updated_teacher as (
            update teachers t
            set
                hired_at = @HiredAt,
                active_from = @ActiveFrom,
                current_status = @Status::person_status,
                updated_at = @UpdatedAt
            from target x
            where t.id = x.id
            returning t.id, t.organization_id, t.person_id, t.hired_at, t.active_from, t.current_status, t.created_at, t.updated_at
        )
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
        from updated_teacher t
        join updated_person p on p.id = t.person_id;
        ";

    public const string Delete = @"
        with target as (
            select t.person_id
            from teachers t
            join persons p on p.id = t.person_id and p.deleted_at is null
            where t.organization_id = @OrganizationId and t.id = @Id
        ), updated_teacher as (
            update teachers t
            set current_status = 'inactive'::person_status, updated_at = now()
            from target x
            where t.person_id = x.person_id
            returning t.person_id
        )
        update persons p
        set status = 'inactive'::person_status, deleted_at = now(), updated_at = now()
        from updated_teacher t
        where p.id = t.person_id;
        ";
}
