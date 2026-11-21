using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Services.Notifications;

public sealed class FirebasePushSender : IPushSender, IDisposable
{
    private readonly Lazy<FirebaseApp> _app = new(() => FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault()
    }, "kiai-push"));

    public async Task<PushSendResult> SendAsync(PushDelivery d, CancellationToken ct)
    {
        try
        {
            await FirebaseMessaging.GetMessaging(_app.Value).SendAsync(new Message
            {
                Fid = d.Token,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = d.Title, Body = d.Body },
                Data = new Dictionary<string, string> { ["notificationId"] = d.NotificationId.ToString(), ["destination"] = "notifications" },
                Android = new AndroidConfig { Notification = new AndroidNotification { ChannelId = "kiai_notifications" } },
                Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } }
            }, ct);
            return PushSendResult.Accepted;
        }
        catch (FirebaseMessagingException e) when (e.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            return PushSendResult.InvalidToken;
        }
        catch (FirebaseMessagingException)
        {
            return PushSendResult.Retry;
        }
    }

    public void Dispose() { if (_app.IsValueCreated) _app.Value.Delete(); }
}
