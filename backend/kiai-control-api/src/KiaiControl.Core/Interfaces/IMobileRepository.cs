using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IMobileRepository
{
    Task<MobileMembership?> MembershipAsync(Guid org, Guid person, CancellationToken ct);
    Task<MobileAccount?> AccountAsync(Guid org, Guid person, CancellationToken ct);
    Task<bool> SaveAccountAsync(Guid org, Guid person, MobileAccount account, CancellationToken ct);
    Task<bool> SavePhotoAsync(Guid org, Guid person, byte[]? photo, string? contentType, CancellationToken ct);
    Task<MobileProgress> ProgressAsync(Guid org, Guid client, CancellationToken ct);
    Task<IReadOnlyList<AttendanceRanking>> RankingAsync(Guid org, Guid client, bool yearOnly, int page, int size, CancellationToken ct);
    Task<bool> RegisterDeviceAsync(Guid org, Guid person, string installation, string token, string platform, CancellationToken ct);
    Task RemoveDeviceAsync(Guid org, Guid person, string installation, CancellationToken ct);
}

public interface IPushSender
{
    Task<PushSendResult> SendAsync(PushDelivery delivery, CancellationToken ct);
}

public enum PushSendResult { Accepted, InvalidToken, Retry }

public interface IPushDeliveryRepository
{
    Task<IReadOnlyList<PushDelivery>> ClaimAsync(CancellationToken ct);
    Task CompleteAsync(PushDelivery delivery, PushSendResult result, CancellationToken ct);
}
