namespace KiaiControl.Core.Entities;
public sealed class UserAccountCandidate
{
    public bool HasClientProfile { get; init; }
    public bool HasTeacherProfile { get; init; }
    public Guid PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
}
