begin;

drop index if exists uq_persons_org_email;
create unique index uq_persons_org_email
    on persons (organization_id, email)
    where email is not null and deleted_at is null;

drop index if exists uq_persons_org_document;
create unique index uq_persons_org_document
    on persons (organization_id, document)
    where document is not null and deleted_at is null;

commit;