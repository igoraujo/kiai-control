namespace KiaiControl.Core.Entities;
public sealed class AuthUser
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int SessionVersion { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public string[] Roles { get; set; } = [];
    public string[] Permissions { get; set; } = [];
}
