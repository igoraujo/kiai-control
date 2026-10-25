using KiaiControl.Contracts.Fitness;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Fitness;

public sealed class FitnessUseCase(IProgressionRepository repository)
{
    private static readonly string[] AllowedTypes = ["training_days", "attendance_rate", "load", "body_measurement", "weight", "conditioning", "score", "category", "custom"];

    public async Task<FitnessGoalResponse?> CreateAsync(
        Guid organizationId,
        CreateFitnessGoalRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || request.ClientId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ClientId are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160
            || request.Description?.Length > 5000 || request.TargetValue <= 0
            || request.StartsAt == default || request.DueDate < request.StartsAt
            || request.Unit?.Length > 30)
        {
            throw new ArgumentException("Fitness goal fields are invalid.");
        }

        var goalType = AllowedTypes.FirstOrDefault(type => string.Equals(type, request.GoalType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (goalType is null)
        {
            throw new ArgumentException("GoalType is not supported.");
        }

        var goal = await repository.CreateFitnessGoalAsync(new FitnessGoal
        {
            OrganizationId = organizationId,
            ClientId = request.ClientId,
            ModalityId = request.ModalityId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            GoalType = goalType,
            TargetValue = request.TargetValue,
            Unit = Normalize(request.Unit),
            StartsAt = request.StartsAt,
            DueDate = request.DueDate
        }, cancellationToken);
        return goal is null ? null : ToResponse(goal);
    }

    public async Task<IReadOnlyList<FitnessGoalResponse>> ListAsync(
        Guid organizationId,
        Guid? clientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var goals = await repository.ListFitnessGoalsAsync(organizationId, clientId, page, pageSize, cancellationToken);
        return goals.Select(ToResponse).ToList();
    }

    public async Task<FitnessProgressResponse?> RecordProgressAsync(
        Guid organizationId,
        Guid goalId,
        Guid? teacherId,
        RecordFitnessProgressRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || goalId == Guid.Empty || request.RecordedValue < 0 || request.Notes?.Length > 255)
        {
            throw new ArgumentException("Progress record fields are invalid.");
        }

        var record = await repository.RecordFitnessProgressAsync(organizationId, new FitnessProgressRecord
        {
            FitnessGoalId = goalId,
            RecordedValue = request.RecordedValue,
            Notes = Normalize(request.Notes),
            CreatedByTeacherId = teacherId
        }, cancellationToken);
        return record is null ? null : new FitnessProgressResponse
        {
            Id = record.Id,
            FitnessGoalId = record.FitnessGoalId,
            RecordedValue = record.RecordedValue,
            Notes = record.Notes,
            RecordedAt = record.RecordedAt,
            CreatedByTeacherId = record.CreatedByTeacherId
        };
    }

    public Task<bool> CancelAsync(Guid organizationId, Guid goalId, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (goalId == Guid.Empty)
        {
            throw new ArgumentException("GoalId is required.");
        }

        return repository.CancelFitnessGoalAsync(organizationId, goalId, cancellationToken);
    }

    public async Task<FitnessSummaryResponse> SummaryAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("ClientId is required.");
        }

        var goals = await repository.ListFitnessGoalsAsync(organizationId, clientId, 1, 200, cancellationToken);
        var responses = goals.Select(ToResponse).ToList();
        return new FitnessSummaryResponse
        {
            ClientId = clientId,
            ActiveGoals = responses.Count(goal => goal.Status == "active"),
            AchievedGoals = responses.Count(goal => goal.Status == "achieved"),
            Goals = responses
        };
    }

    private static void EnsureOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static FitnessGoalResponse ToResponse(FitnessGoal goal) => new()
    {
        Id = goal.Id,
        OrganizationId = goal.OrganizationId,
        ClientId = goal.ClientId,
        ModalityId = goal.ModalityId,
        Title = goal.Title,
        Description = goal.Description,
        GoalType = goal.GoalType,
        TargetValue = goal.TargetValue,
        CurrentValue = goal.CurrentValue,
        Unit = goal.Unit,
        StartsAt = goal.StartsAt,
        DueDate = goal.DueDate,
        Status = goal.Status,
        CreatedAt = goal.CreatedAt
    };
}