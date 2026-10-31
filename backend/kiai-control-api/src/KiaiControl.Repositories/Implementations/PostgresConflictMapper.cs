using KiaiControl.Core.Common;
using Npgsql;

namespace KiaiControl.Repositories.Implementations;

internal static class PostgresConflictMapper
{
    public static ConflictException ToConflict(PostgresException exception) => exception.ConstraintName switch
    {
        "uq_persons_org_email" => new ConflictException("Email is already registered in this organization."),
        "uq_persons_org_document" => new ConflictException("Document is already registered in this organization."),
        "uq_clients_org_registration" => new ConflictException("RegistrationCode is already in use in this organization."),
        "uq_plans_org_name" => new ConflictException("A billing plan with this name already exists in this organization."),
        _ => new ConflictException("A record with the same unique value already exists.")
    };
}