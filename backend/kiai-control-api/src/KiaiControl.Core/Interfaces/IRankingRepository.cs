using KiaiControl.Core.Entities;

namespace KiaiControl.Core.Interfaces;

public interface IRankingRepository
{
    Task<RankingCycle?> CreateCycleAsync(RankingCycle cycle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingCycle>> ListCyclesAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingEntry>?> RecalculateAsync(Guid organizationId, Guid cycleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingEntry>> GetCurrentRankingAsync(Guid organizationId, DateOnly today, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingEntry>> GetRankingByCycleAsync(Guid organizationId, Guid cycleId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Medal?> CreateMedalAsync(Medal medal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Medal>> ListMedalsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<PersonMedal?> AwardMedalAsync(PersonMedal award, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonMedal>> ListPersonMedalsAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonMedal>> ListClientMedalsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default);
}