namespace KiaiControl.Core.Entities;

public sealed class Teacher : Person
{
    public Guid Id { get; set; }

    public DateOnly? HiredAt { get; set; }

    public DateOnly? ActiveFrom { get; set; }
}
