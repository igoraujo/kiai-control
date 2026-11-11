namespace KiaiControl.Contracts.Administration;
public sealed class DirectoryPersonResponse
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

public sealed class SavePersonRequest
{
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class AccountStateRequest
{
    public bool IsActive { get; set; }
}

public sealed class TemporaryPasswordRequest
{
    public string Password { get; set; } = "";
}

public sealed class OrganizationSettingsResponse
{
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public string CurrencyCode { get; set; } = "BRL";
    public bool AllowClientCheckin { get; set; }
    public bool RequireTeacherConfirmation { get; set; } = true;
    public int DefaultLateToleranceMinutes { get; set; } = 10;
}

public sealed class AccountEmailRequest
{
    public string Email { get; set; } = "";
}

public sealed record PersonOptionResponse(Guid PersonId,string FullName);
