namespace KiaiControl.Admin.Components.Common;

public sealed class PersonFormModel
{
    public Guid? BranchId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? PreferredName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? Document { get; set; }

    public string? MedicalNotes { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public bool HideFromRanking { get; set; }

    public string Status { get; set; } = "active";
}