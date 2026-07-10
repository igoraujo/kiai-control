namespace KiaiControl.Core.Entities;

public sealed class AuthUser
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }

    public Guid OrganizationId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
