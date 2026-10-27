namespace KiaiControl.Core.Entities;

public abstract class Person
{
    public Guid OrganizationId { get; set; }

    public Guid PersonId { get; set; }

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

    public string Status { get; set; } = "active";

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}