namespace KiaiControl.Core.Entities;

public sealed class GraduationSystem
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ModalityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}