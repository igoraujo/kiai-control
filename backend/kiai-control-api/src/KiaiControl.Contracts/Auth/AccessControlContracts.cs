namespace KiaiControl.Contracts.Auth;

public sealed class RoleResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class PermissionResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class AssignRoleRequest
{
    public string RoleCode { get; set; } = string.Empty;
}

public sealed class SetRolePermissionRequest
{
    public string PermissionCode { get; set; } = string.Empty;
}

public sealed class CreateUserAccountRequest
{
    public string Email { get; set; } = string.Empty;

    public string TemporaryPassword { get; set; } = string.Empty;

    public string RoleCode { get; set; } = string.Empty;
}

public sealed class UserAccountCandidateResponse
{
    public Guid PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? Email { get; init; }
}

public sealed class UserAccountResponse
{
    public Guid PersonId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;
}