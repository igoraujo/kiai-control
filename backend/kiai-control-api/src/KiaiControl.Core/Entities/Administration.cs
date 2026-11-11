namespace KiaiControl.Core.Entities;
public sealed class DirectoryPerson
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "active";
    public Guid? ClientId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? UserId { get; set; }
    public string? LoginEmail { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public string[] Roles { get; set; } = [];
}

public sealed class OrganizationSettings
{
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public string CurrencyCode { get; set; } = "BRL";
    public bool AllowClientCheckin { get; set; }
    public bool RequireTeacherConfirmation { get; set; } = true;
    public int DefaultLateToleranceMinutes { get; set; } = 10;
}
