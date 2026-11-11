using KiaiControl.Api.Configuration;
using KiaiControl.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace KiaiControl.Api.Services;

public sealed class PlatformTelemetryWorker(IServiceScopeFactory scopeFactory, PlatformTelemetryBuffer buffer,
    IOptions<PlatformPrivacyOptions> options, ILogger<PlatformTelemetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.FlushSeconds));
        var nextRetention = DateTimeOffset.UtcNow;
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
                if (DateTimeOffset.UtcNow < nextRetention) continue;
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var policy = options.Value;
                    var now = DateTimeOffset.UtcNow;
                    for (var batch = 0; batch < 10; batch++)
                    {
                        var result = await scope.ServiceProvider.GetRequiredService<IPlatformObservabilityRepository>().ApplyRetentionAsync(
                            now.AddDays(-policy.MetricsRetentionDays), now.AddDays(-policy.IncidentRetentionDays),
                            now.AddDays(-policy.AuditIdentityRetentionDays), stoppingToken);
                        if (result.MetricsDeleted < 2000 && result.IncidentsDeleted < 2000 && result.AuditIdentitiesRedacted < 2000) break;
                        if (batch == 9) logger.LogWarning("Platform retention reached the batch limit. Review backlog and retention capacity.");
                    }
                    nextRetention = now.AddHours(1);
                }
                catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(e, "Platform retention failed. It will be retried.");
                    nextRetention = DateTimeOffset.UtcNow.AddMinutes(5);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var batch = buffer.Drain();
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IPlatformObservabilityRepository>().SaveMetricsAsync(batch, ct);
        }
        catch (Exception exception)
        {
            foreach (var item in batch) buffer.Add(item);
            if (!ct.IsCancellationRequested) logger.LogError(exception, "Platform metrics persistence failed. Aggregates remain buffered.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(cancellationToken);
    }
}
