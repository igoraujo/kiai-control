namespace KiaiControl.Core.Entities;

public sealed class EligibleAttendanceClient
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;
}
