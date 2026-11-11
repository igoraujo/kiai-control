using KiaiControl.Contracts.Platform;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Platform;

public sealed class PlatformObservabilityUseCase(IPlatformObservabilityRepository repository)
{
    public async Task<PlatformMetricsResponse> DashboardAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        PlatformAdministrationUseCase.ValidateFilter(filter);
        if (filter.To - filter.From > TimeSpan.FromDays(90))
            throw new ArgumentException("Os indicadores permitem um período de até 90 dias.");
        var result = await repository.DashboardAsync(filter, ct);
        var s = result.Summary;
        return new()
        {
            Requests = s.Requests, ServerErrors = s.ServerErrors, ClientErrors = s.ClientErrors,
            AverageDurationMs = s.AverageDurationMs, MaxDurationMs = s.MaxDurationMs,
            Active = s.Active, Suspended = s.Suspended, Inactive = s.Inactive, Archived = s.Archived,
            PrivacyHeld = s.PrivacyHeld, WithoutActiveOwner = s.WithoutActiveOwner,
            Days = result.Days.Select(x => new PlatformMetricDayResponse(x.Day, x.Requests, x.ServerErrors, x.AverageDurationMs)).ToArray(),
            Academies = result.Academies.Items.Select(x => new PlatformAcademyControlResponse(x.Id, x.Name, x.Status,
                x.Archived, x.PrivacyHold, x.Accounts, x.ActiveOwners, x.BlockedAccounts, x.PendingPasswords,
                x.Requests, x.ServerErrors, x.Incidents, x.LastActivityAt)).ToArray(),
            TotalAcademies = result.Academies.Total,
            Routes = result.Routes.Select(x => new PlatformRouteMetricResponse(x.Route, x.Method, x.Requests, x.ServerErrors, x.AverageDurationMs)).ToArray()
        };
    }
}
