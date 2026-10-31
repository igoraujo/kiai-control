using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Modalities;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlModalityScheduleRepository(IDbConnectionFactory dbConnectionFactory) : IModalityScheduleRepository
{
    public async Task<ModalitySchedule?> CreateAsync(
        ModalitySchedule schedule,
        CancellationToken cancellationToken = default)
    {
        schedule.Id = Guid.NewGuid();
        schedule.CreatedAt = DateTimeOffset.UtcNow;

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ModalitySchedule>(ModalityScheduleQueries.Insert, schedule);
    }

    public async Task<IReadOnlyList<ModalitySchedule>> ListAsync(
        Guid organizationId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var schedules = await connection.QueryAsync<ModalitySchedule>(
            ModalityScheduleQueries.List,
            new { OrganizationId = organizationId, ModalityId = modalityId });
        return schedules.ToList();
    }

    public async Task<bool> ModalityExistsAsync(
        Guid organizationId,
        Guid modalityId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<bool>(
            ModalityScheduleQueries.GetModality,
            new { OrganizationId = organizationId, ModalityId = modalityId });
    }

    public async Task<bool> DeactivateAsync(
        Guid organizationId,
        Guid modalityId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            ModalityScheduleQueries.Deactivate,
            new
            {
                OrganizationId = organizationId,
                ModalityId = modalityId,
                ScheduleId = scheduleId,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        return affected > 0;
    }
}
