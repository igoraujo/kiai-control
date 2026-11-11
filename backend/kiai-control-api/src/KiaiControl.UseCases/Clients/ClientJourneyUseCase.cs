using KiaiControl.Contracts.Clients;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Catalog;

namespace KiaiControl.UseCases.Clients;
public sealed class ClientJourneyUseCase(IClientJourneyRepository repository)
{
    public Task<Guid?> GetGoalClientIdAsync(Guid org, Guid id, CancellationToken ct) => repository.GetGoalClientIdAsync(org, id, ct);
    public async Task<ClientJourneyResponse?> GetAsync(Guid org, Guid clientId, int page, int size, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, page, size, null);
        if (clientId == Guid.Empty)
            throw new ArgumentException("Aluno inválido.");
        var r = await repository.GetAsync(org, clientId, page, size, ct);
        if (r is null)
            return null;
        return new(new(r.Attendance.Items.Select(a => new AttendanceTimelineResponse(a.LessonId, a.ModalityName, a.StartsAt, a.Status)).ToArray(), r.Attendance.Total), r.Graduations.Select(g => new GraduationTimelineResponse(g.ModalityName, g.LevelName, g.EventDate, g.EventType)).ToArray(), r.Medals.Select(m => new ClientMedalResponse(m.Name, m.Reason, m.AwardedAt)).ToArray(), r.Rankings.Select(r => new ClientRankingResponse(r.CycleName, r.Position, r.Points, r.StreakCount)).ToArray(), r.PresentCount, r.CompletedCount);
    }
}
