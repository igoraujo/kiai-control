using KiaiControl.Contracts.Ranking;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Ranking;

public sealed class RankingUseCase(IRankingRepository repository)
{
    public async Task<RankingCycleResponse?> CreateCycleAsync(
        Guid organizationId,
        CreateRankingCycleRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120
            || request.StartsAt == default || request.EndsAt < request.StartsAt)
        {
            throw new ArgumentException("Ranking cycle fields are invalid.");
        }

        var cycle = await repository.CreateCycleAsync(new RankingCycle
        {
            OrganizationId = organizationId,
            ModalityId = request.ModalityId,
            ClassGroupId = request.ClassGroupId,
            Name = request.Name.Trim(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        }, cancellationToken);
        return cycle is null ? null : ToResponse(cycle);
    }

    public async Task<IReadOnlyList<RankingCycleResponse>> ListCyclesAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var cycles = await repository.ListCyclesAsync(organizationId, page, pageSize, cancellationToken);
        return cycles.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RankingEntryResponse>?> RecalculateAsync(Guid organizationId, Guid cycleId, CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, cycleId);
        var entries = await repository.RecalculateAsync(organizationId, cycleId, cancellationToken);
        return entries?.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RankingEntryResponse>> GetCurrentAsync(Guid organizationId, DateOnly today, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var entries = await repository.GetCurrentRankingAsync(organizationId, today, page, pageSize, cancellationToken);
        return entries.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RankingEntryResponse>> GetByCycleAsync(Guid organizationId, Guid cycleId, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, cycleId);
        var entries = await repository.GetRankingByCycleAsync(organizationId, cycleId, page, pageSize, cancellationToken);
        return entries.Select(ToResponse).ToList();
    }

    public async Task<MedalResponse?> CreateMedalAsync(Guid organizationId, CreateMedalRequest request, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 60
            || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120
            || request.Description?.Length > 255 || request.PointsBonus < 0)
        {
            throw new ArgumentException("Medal fields are invalid.");
        }

        var medal = await repository.CreateMedalAsync(new Medal
        {
            OrganizationId = organizationId,
            Code = request.Code.Trim().ToLowerInvariant(),
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            PointsBonus = request.PointsBonus,
            IsActive = true
        }, cancellationToken);
        return medal is null ? null : ToResponse(medal);
    }

    public async Task<IReadOnlyList<MedalResponse>> ListMedalsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var medals = await repository.ListMedalsAsync(organizationId, cancellationToken);
        return medals.Select(ToResponse).ToList();
    }

    public async Task<PersonMedalResponse?> AwardMedalAsync(Guid organizationId, Guid medalId, AwardMedalRequest request, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || medalId == Guid.Empty || request.PersonId == Guid.Empty || request.Reason?.Length > 255)
        {
            throw new ArgumentException("Medal award fields are invalid.");
        }

        var award = await repository.AwardMedalAsync(new PersonMedal
        {
            OrganizationId = organizationId,
            PersonId = request.PersonId,
            MedalId = medalId,
            Reason = Normalize(request.Reason)
        }, cancellationToken);
        return award is null ? null : ToResponse(award);
    }

    public async Task<IReadOnlyList<PersonMedalResponse>> ListPersonMedalsAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, personId);
        var awards = await repository.ListPersonMedalsAsync(organizationId, personId, cancellationToken);
        return awards.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<PersonMedalResponse>> ListClientMedalsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken)
    {
        EnsureIds(organizationId, clientId);
        var awards = await repository.ListClientMedalsAsync(organizationId, clientId, cancellationToken);
        return awards.Select(ToResponse).ToList();
    }

    private static void EnsureOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }
    }

    private static void EnsureIds(Guid organizationId, Guid id)
    {
        EnsureOrganization(organizationId);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RankingCycleResponse ToResponse(RankingCycle value) => new()
    {
        Id = value.Id,
        OrganizationId = value.OrganizationId,
        ModalityId = value.ModalityId,
        ClassGroupId = value.ClassGroupId,
        Name = value.Name,
        StartsAt = value.StartsAt,
        EndsAt = value.EndsAt,
        Status = value.Status,
        CreatedAt = value.CreatedAt
    };

    private static RankingEntryResponse ToResponse(RankingEntry value) => new()
    {
        RankingCycleId = value.RankingCycleId,
        ClientId = value.ClientId,
        ClientName = value.ClientName,
        Points = value.Points,
        AttendanceCount = value.AttendanceCount,
        StreakCount = value.StreakCount,
        Position = value.Position
    };

    private static MedalResponse ToResponse(Medal value) => new()
    {
        Id = value.Id,
        OrganizationId = value.OrganizationId,
        Code = value.Code,
        Name = value.Name,
        Description = value.Description,
        PointsBonus = value.PointsBonus,
        IsActive = value.IsActive,
        CreatedAt = value.CreatedAt
    };

    private static PersonMedalResponse ToResponse(PersonMedal value) => new()
    {
        Id = value.Id,
        OrganizationId = value.OrganizationId,
        PersonId = value.PersonId,
        MedalId = value.MedalId,
        AwardedAt = value.AwardedAt,
        Reason = value.Reason
    };
}