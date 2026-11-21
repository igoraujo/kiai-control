using KiaiControl.Core.Interfaces;

namespace KiaiControl.Api.Services;

public sealed class PushDeliveryWorker(IServiceScopeFactory scopes, IPushSender sender, ILogger<PushDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IPushDeliveryRepository>();
                foreach (var delivery in await repository.ClaimAsync(stoppingToken))
                {
                    try
                    {
                        var result = await sender.SendAsync(delivery, stoppingToken);
                        await repository.CompleteAsync(delivery, result, stoppingToken);
                        if (result == PushSendResult.Retry) logger.LogWarning("Push {NotificationId} aguardando nova tentativa.", delivery.NotificationId);
                    }
                    catch (Exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Falha ao entregar push {NotificationId}; lease permite nova tentativa.", delivery.NotificationId);
                    }
                }
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Não foi possível consultar a fila de push.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
