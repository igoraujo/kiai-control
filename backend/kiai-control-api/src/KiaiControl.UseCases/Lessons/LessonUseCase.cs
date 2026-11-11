using KiaiControl.Contracts.Lessons;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Lessons;
public sealed class LessonUseCase(ILessonRepository lessonRepository)
{
    public async Task<LessonResponse?> CreateAsync(Guid organizationId, CreateLessonRequest request, CancellationToken cancellationToken)
    {
        if (request.ModalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.ModalityIdIsRequired);
        }

        ValidateInterval(organizationId, request.StartsAt, request.EndsAt);
        var lesson = new Lesson
        {
            OrganizationId = organizationId,
            ModalityId = request.ModalityId,
            StartsAt = request.StartsAt.ToUniversalTime(),
            EndsAt = request.EndsAt.ToUniversalTime(),
            Status = "scheduled"
        };
        var created = await lessonRepository.CreateAsync(lesson, cancellationToken);
        return created is null ? null : ToResponse(created);
    }

    public async Task<IReadOnlyList<LessonResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken, Guid? teacherId = null, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        if (from is not null && to is not null && from >= to)
        {
            throw new ArgumentException(SystemMessages.TheEndOfTheLessonRangeMustBeLaterThanItsStart);
        }

        var lessons = await lessonRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken, teacherId, from, to);
        return lessons.Select(ToResponse).ToList();
    }

    public async Task<LessonResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return lesson is null ? null : ToResponse(lesson);
    }

    public async Task<LessonResponse?> GetByIdForTeacherAsync(Guid organizationId, Guid id, Guid teacherId, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetByIdForTeacherAsync(organizationId, id, teacherId, cancellationToken);
        return lesson is null ? null : ToResponse(lesson);
    }

    public async Task<LessonResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateLessonRequest request, CancellationToken cancellationToken)
    {
        ValidateInterval(organizationId, request.StartsAt, request.EndsAt);
        var updated = await lessonRepository.UpdateAsync(new Lesson { Id = id, OrganizationId = organizationId, StartsAt = request.StartsAt.ToUniversalTime(), EndsAt = request.EndsAt.ToUniversalTime() }, cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    public async Task<LessonResponse?> CancelAsync(Guid organizationId, Guid id, CancelLessonRequest request, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }

        if (request.Reason?.Length > 255)
        {
            throw new ArgumentException(SystemMessages.ReasonMustNotExceed255Characters);
        }

        var canceled = await lessonRepository.CancelAsync(organizationId, id, string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(), cancellationToken);
        return canceled is null ? null : ToResponse(canceled);
    }

    public async Task<LessonResponse?> RescheduleAsync(Guid organizationId, Guid id, RescheduleLessonRequest request, CancellationToken cancellationToken)
    {
        ValidateInterval(organizationId, request.StartsAt, request.EndsAt);
        var rescheduled = await lessonRepository.RescheduleAsync(organizationId, id, request.StartsAt.ToUniversalTime(), request.EndsAt.ToUniversalTime(), cancellationToken);
        return rescheduled is null ? null : ToResponse(rescheduled);
    }

    public async Task<IReadOnlyList<LessonResponse>?> GenerateFromModalitySchedulesAsync(Guid organizationId, GenerateModalityLessonsRequest request, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || request.ModalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndModalityIdAreRequired);
        }

        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException(SystemMessages.EndDateMustBeOnOrAfterStartDate);
        }

        if (request.EndDate.DayNumber - request.StartDate.DayNumber > 90)
        {
            throw new ArgumentException(SystemMessages.TheGenerationRangeCannotExceed90Days);
        }

        var lessons = await lessonRepository.GenerateFromModalitySchedulesAsync(organizationId, request.ModalityId, request.StartDate, request.EndDate, cancellationToken);
        return lessons?.Select(ToResponse).ToList();
    }

    private static void ValidateInterval(Guid organizationId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }

        if (endsAt <= startsAt)
        {
            throw new ArgumentException(SystemMessages.EndsAtMustBeLaterThanStartsAt);
        }
    }

    internal static LessonResponse ToResponse(Lesson lesson)
    {
        return new LessonResponse
        {
            Id = lesson.Id,
            OrganizationId = lesson.OrganizationId,
            ModalityId = lesson.ModalityId,
            ModalityScheduleId = lesson.ModalityScheduleId,
            ModalityName = lesson.ModalityName,
            BranchName = lesson.BranchName,
            StartsAt = lesson.StartsAt,
            EndsAt = lesson.EndsAt,
            Status = lesson.Status,
            CancellationReason = lesson.CancellationReason,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }
}
