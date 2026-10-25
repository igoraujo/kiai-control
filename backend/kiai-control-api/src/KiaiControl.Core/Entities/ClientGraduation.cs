namespace KiaiControl.Core.Entities;

public sealed class ClientGraduation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ClientId { get; set; }

    public Guid ModalityId { get; set; }

    public Guid CurrentLevelId { get; set; }

    public DateOnly GrantedAt { get; set; }

    public Guid? GrantedByTeacherId { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}