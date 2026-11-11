using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;
public interface IClientJourneyRepository
{
    Task<Guid?> GetGoalClientIdAsync(Guid organizationId, Guid goalId, CancellationToken ct);
    Task<ClientJourney?> GetAsync(Guid organizationId, Guid clientId, int page, int size, CancellationToken ct);
}
