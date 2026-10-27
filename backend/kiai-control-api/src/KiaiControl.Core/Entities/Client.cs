namespace KiaiControl.Core.Entities;

public sealed class Client : Person
{
    public Guid Id { get; set; }

    public string? RegistrationCode { get; set; }

    public DateOnly? JoinedAt { get; set; }
}
