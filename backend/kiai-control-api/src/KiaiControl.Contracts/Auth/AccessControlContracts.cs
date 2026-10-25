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