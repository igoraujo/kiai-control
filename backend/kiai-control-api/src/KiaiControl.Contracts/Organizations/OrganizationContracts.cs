namespace KiaiControl.Contracts.Organizations;

public sealed class RegisterOrganizationRequest
{
    public string Name { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    public string? Document { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string OwnerPassword { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    public string? Document { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}

public sealed class OrganizationResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? TradeName { get; init; }

    public string? Document { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class OrganizationRegistrationResponse
{
    public OrganizationResponse Organization { get; init; } = new();

    public Guid OwnerUserId { get; init; }

    public string OwnerEmail { get; init; } = string.Empty;
}