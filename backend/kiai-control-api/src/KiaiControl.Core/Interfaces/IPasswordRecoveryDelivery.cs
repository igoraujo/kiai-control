namespace KiaiControl.Core.Interfaces;
public interface IPasswordRecoveryDelivery
{
    bool IsConfigured { get; }

    Task SendAsync(string email, string token, CancellationToken ct);
}
