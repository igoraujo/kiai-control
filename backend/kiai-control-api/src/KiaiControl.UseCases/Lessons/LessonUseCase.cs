using KiaiControl.Contracts.Lessons;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Lessons;

public sealed class LessonUseCase(ILessonRepository lessonRepository)
{
    public async Task<LessonResponse?> CreateAsync(
        Guid organizationId,
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        ValidateInterval(organizationId, request.ClassGroupId, request.StartsAt, request.EndsAt);

        var lesson = new Lesson
        {
            OrganizationId = organizationId,
            ClassGroupId = request.ClassGroupId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = "scheduled"
        };

        var created = await lessonRepository.CreateAsync(lesson, cancellationToken);
        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<LessonResponse>> ListAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var lessons = await lessonRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return lessons.Select(ToResponse).ToList();
    }

    public async Task<LessonResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return lesson is null ? null : ToResponse(lesson);
    }

    public async Task<LessonResponse?> UpdateAsync(
        Guid organizationId,
        Guid id,
        UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        ValidateInterval(organizationId, Guid.NewGuid(), request.StartsAt, request.EndsAt, checkClassGroup: false);

        var updated = await lessonRepository.UpdateAsync(new Lesson
        {
            Id = id,
            OrganizationId = organizationId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        }, cancellationToken);

        return updated is null ? null : ToResponse(updated);
    }

    public async Task<LessonResponse?> CancelAsync(
        Guid organizationId,
        Guid id,
        CancelLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId is required.");
        }

        if (request.Reason?.Length > 255)
        {
            throw new ArgumentException("Reason must not exceed 255 characters.");
        }

        var canceled = await lessonRepository.CancelAsync(
            organizationId,
            id,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            cancellationToken);
        return canceled is null ? null : ToResponse(canceled);
    }

    public async Task<LessonResponse?> RescheduleAsync(
        Guid organizationId,
        Guid id,
        RescheduleLessonRequest request,
        CancellationToken cancellationToken)
    {
        ValidateInterval(organizationId, Guid.NewGuid(), request.StartsAt, request.EndsAt, checkClassGroup: false);

        var rescheduled = await lessonRepository.RescheduleAsync(
            organizationId,
            id,
            request.StartsAt,
            request.EndsAt,
            cancellationToken);
        return rescheduled is null ? null : ToResponse(rescheduled);
    }

    public async Task<IReadOnlyList<LessonResponse>?> GenerateFromSchedulesAsync(
        Guid organizationId,
        GenerateLessonsRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || request.ClassGroupId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId and ClassGroupId are required.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("EndDate must be on or after StartDate.");
        }

        if (request.EndDate.DayNumber - request.StartDate.DayNumber > 90)
        {
            throw new ArgumentException("The generation range cannot exceed 90 days.");
        }

        var lessons = await lessonRepository.GenerateFromSchedulesAsync(
            organizationId,
            request.ClassGroupId,
            request.StartDate,
            request.EndDate,
            cancellationToken);
        return lessons?.Select(ToResponse).ToList();
    }

    private static void ValidateInterval(
        Guid organizationId,
        Guid classGroupId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        bool checkClassGroup = true)
    {
        if (organizationId == Guid.Empty || (checkClassGroup && classGroupId == Guid.Empty))
        {
            throw new ArgumentException("OrganizationId and ClassGroupId are required.");
        }

        if (endsAt <= startsAt)
        {
            throw new ArgumentException("EndsAt must be later than StartsAt.");
        }
    }

    private static LessonResponse ToResponse(Lesson lesson)
    {
        return new LessonResponse
        {
            Id = lesson.Id,
            OrganizationId = lesson.OrganizationId,
            ClassGroupId = lesson.ClassGroupId,
            StartsAt = lesson.StartsAt,
            EndsAt = lesson.EndsAt,
            Status = lesson.Status,
            CancellationReason = lesson.CancellationReason,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }
}