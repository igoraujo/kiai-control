using KiaiControl.Core.Entities;

namespace KiaiControl.Api.Services;

public sealed class PlatformTelemetryBuffer
{
    private readonly object _gate = new();
    private readonly Dictionary<(DateTimeOffset, Guid?, string, string, int), PlatformRequestMetric> _metrics = [];
    public long DroppedMeasurements { get; private set; }

    public void Add(PlatformRequestMetric metric)
    {
        lock (_gate)
        {
            var key = (metric.BucketStart, metric.OrganizationId, metric.Route, metric.Method, metric.StatusCode);
            if (_metrics.TryGetValue(key, out var current))
            {
                current.Requests += metric.Requests;
                current.DurationTotalMs += metric.DurationTotalMs;
                current.MaxDurationMs = Math.Max(current.MaxDurationMs, metric.MaxDurationMs);
            }
            else if (_metrics.Count < 20_000) _metrics[key] = metric;
            else DroppedMeasurements += metric.Requests;
        }
    }

    public IReadOnlyList<PlatformRequestMetric> Drain()
    {
        lock (_gate)
        {
            var result = _metrics.Values.ToArray();
            _metrics.Clear();
            return result;
        }
    }
}
