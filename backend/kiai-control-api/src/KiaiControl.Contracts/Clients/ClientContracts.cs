namespace KiaiControl.Contracts.Clients;

public sealed class CreateClientRequest
{
    public Guid OrganizationId { get; set; }

    public Guid? BranchId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? PreferredName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Document { get; set; }

    public string? MedicalNotes { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public bool HideFromRanking { get; set; }

    public string? RegistrationCode { get; set; }

    public DateOnly? JoinedAt { get; set; }

    public string Status { get; set; } = "active";
}

public sealed class UpdateClientRequest
{
    public Guid? BranchId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? PreferredName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Document { get; set; }

    public string? MedicalNotes { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public bool HideFromRanking { get; set; }

    public string? RegistrationCode { get; set; }

    public DateOnly? JoinedAt { get; set; }

    public string Status { get; set; } = "active";
}

public sealed class ClientResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public Guid PersonId { get; init; }

    public Guid? BranchId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? PreferredName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public DateOnly? BirthDate { get; init; }

    public string? Document { get; init; }

    public string? MedicalNotes { get; init; }

    public string? EmergencyContactName { get; init; }

    public string? EmergencyContactPhone { get; init; }

    public bool HideFromRanking { get; init; }

    public string? RegistrationCode { get; init; }

    public DateOnly? JoinedAt { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
