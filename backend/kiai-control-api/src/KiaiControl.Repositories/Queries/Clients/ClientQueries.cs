namespace KiaiControl.Repositories.Queries.Clients;

public static class ClientQueries
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
        ), inserted_client as (
            insert into clients (id, organization_id, person_id, registration_code, joined_at, current_status, created_at)
            select @Id, @OrganizationId, inserted_person.id, @RegistrationCode, @JoinedAt, @Status::person_status, @CreatedAt
            from inserted_person
            returning id, organization_id, person_id, registration_code, joined_at, current_status, created_at, updated_at
        ), assigned_role as (
            insert into person_roles (person_id, role_id)
            select inserted_person.id, roles.id
            from inserted_person
            join roles on roles.code = 'client'::role_code
            returning person_id
        )
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
            c.updated_at as UpdatedAt
        from inserted_client c
        join inserted_person p on p.id = c.person_id
        join assigned_role ar on ar.person_id = c.person_id;
        ";

    public const string ListByOrganization = @"
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
            c.updated_at as UpdatedAt
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId and p.deleted_at is null
        order by c.created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
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
            c.updated_at as UpdatedAt
        from clients c
        join persons p on p.id = c.person_id
        where c.organization_id = @OrganizationId and c.id = @Id and p.deleted_at is null;
        ";

    public const string Update = @"
        with target as (
            select c.id, c.organization_id, c.person_id
            from clients c
            join persons p on p.id = c.person_id and p.deleted_at is null
            where c.organization_id = @OrganizationId and c.id = @Id
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
        ), updated_client as (
            update clients c
            set
                registration_code = @RegistrationCode,
                joined_at = @JoinedAt,
                current_status = @Status::person_status,
                updated_at = @UpdatedAt
            from target t
            where c.id = t.id
            returning c.id, c.organization_id, c.person_id, c.registration_code, c.joined_at, c.current_status, c.created_at, c.updated_at
        )
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
            c.updated_at as UpdatedAt
        from updated_client c
        join updated_person p on p.id = c.person_id;
        ";

    public const string Delete = @"
        with target as (
            select c.person_id
            from clients c
            join persons p on p.id = c.person_id and p.deleted_at is null
            where c.organization_id = @OrganizationId and c.id = @Id
        ), updated_client as (
            update clients c
            set current_status = 'inactive'::person_status, updated_at = now()
            from target t
            where c.person_id = t.person_id
            returning c.person_id
        )
        update persons p
        set status = 'inactive'::person_status, deleted_at = now(), updated_at = now()
        from updated_client c
        where p.id = c.person_id;
        ";
}
