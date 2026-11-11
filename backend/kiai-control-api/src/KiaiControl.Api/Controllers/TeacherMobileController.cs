using System.Security.Claims;
using KiaiControl.Contracts.Attendance;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Attendance;
using KiaiControl.UseCases.Lessons;
using KiaiControl.UseCases.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "teacher")]
[Route("api/v1/mobile/teacher")]
public sealed class TeacherMobileController(
    OrganizationContext organizationContext,
    TeacherUseCase teacherUseCase,
    LessonUseCase lessonUseCase,
    AttendanceUseCase attendanceUseCase) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<TeacherPortalProfileResponse>> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        var (organizationId, teacher) = await GetActiveTeacherAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (teacher is null)
        {
            return NotFound();
        }

        return Ok(ToProfile(teacher));
    }

    [HttpGet("lessons")]
    [Authorize(Policy = PermissionCodes.LessonsManage)]
    public async Task<ActionResult<IReadOnlyList<TeacherPortalLessonResponse>>> GetLessonsAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var (organizationId, teacher) = await GetActiveTeacherAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (teacher is null)
        {
            return NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var rangeStart = from?.ToUniversalTime() ?? now.AddDays(-7);
        var rangeEnd = to?.ToUniversalTime() ?? now.AddDays(24);
        if (rangeEnd <= rangeStart || rangeEnd - rangeStart > TimeSpan.FromDays(31))
        {
            return BadRequest("O período deve ser válido e não pode exceder 31 dias.");
        }

        var lessons = new List<TeacherPortalLessonResponse>();
        const int pageSize = 200;
        for (var page = 1; ; page++)
        {
            var result = await lessonUseCase.ListAsync(
                organizationId,
                page,
                pageSize,
                cancellationToken,
                teacherId: teacher.Id,
                from: rangeStart,
                to: rangeEnd);
            lessons.AddRange(result
                .Where(lesson => lesson.Status is "scheduled" or "rescheduled" or "completed")
                .Select(lesson => new TeacherPortalLessonResponse
                {
                    Id = lesson.Id,
                    ModalityName = lesson.ModalityName,
                    BranchName = lesson.BranchName,
                    StartsAt = lesson.StartsAt,
                    EndsAt = lesson.EndsAt,
                    Status = lesson.Status
                }));
            if (result.Count < pageSize)
            {
                break;
            }
        }

        return Ok(lessons.OrderBy(lesson => lesson.StartsAt).ToArray());
    }

    [HttpGet("lessons/{lessonId:guid}/eligible-clients")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    public async Task<ActionResult<IReadOnlyList<EligibleAttendanceClientResponse>>> GetEligibleClientsAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var (organizationId, teacher) = await GetActiveTeacherAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (teacher is null)
        {
            return NotFound();
        }

        if (await GetTeacherLessonAsync(organizationId, teacher.Id, lessonId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await attendanceUseCase.ListEligibleClientsByLessonAsync(
            organizationId, lessonId, cancellationToken));
    }

    [HttpGet("lessons/{lessonId:guid}/attendances")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> GetAttendancesAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var (organizationId, teacher) = await GetActiveTeacherAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (teacher is null)
        {
            return NotFound();
        }

        if (await GetTeacherLessonAsync(organizationId, teacher.Id, lessonId, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(await attendanceUseCase.ListByLessonAsync(organizationId, lessonId, cancellationToken));
    }

    [HttpPut("lessons/{lessonId:guid}/attendances")]
    [Authorize(Policy = PermissionCodes.AttendanceManage)]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> SaveAttendancesAsync(
        Guid lessonId,
        [FromBody] SaveAttendanceCallRequest request,
        CancellationToken cancellationToken)
    {
        var (organizationId, teacher) = await GetActiveTeacherAsync(cancellationToken);
        if (organizationId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (teacher is null)
        {
            return NotFound();
        }

        var lesson = await GetTeacherLessonAsync(organizationId, teacher.Id, lessonId, cancellationToken);
        if (lesson is null)
        {
            return NotFound();
        }

        if (!Guid.TryParse(User.FindFirstValue("person_id"), out var validatorPersonId))
        {
            return Unauthorized();
        }

        try
        {
            var saved = await attendanceUseCase.SaveLessonCallAsync(
                organizationId, lessonId, validatorPersonId, request, cancellationToken);
            return saved is null
                ? Conflict(KiaiControl.Api.Common.SystemMessages.TheLessonIsUnavailableOrOneOrMoreClientsAreNotActivelyEnrolledInItsClassGroup)
                : Ok(saved);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private async Task<(Guid OrganizationId, KiaiControl.Contracts.Teachers.TeacherResponse? Teacher)> GetActiveTeacherAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetTeacherContext(out var organizationId, out var personId))
        {
            return (Guid.Empty, null);
        }

        var teacher = await teacherUseCase.GetByPersonIdAsync(
            organizationId, personId, cancellationToken);
        return teacher is null || teacher.Status != "active"
            ? (organizationId, null)
            : (organizationId, teacher);
    }

    private Task<KiaiControl.Contracts.Lessons.LessonResponse?> GetTeacherLessonAsync(
        Guid organizationId,
        Guid teacherId,
        Guid lessonId,
        CancellationToken cancellationToken) =>
        lessonUseCase.GetByIdForTeacherAsync(organizationId, lessonId, teacherId, cancellationToken);

    private bool TryGetTeacherContext(out Guid organizationId, out Guid personId)
    {
        organizationId = organizationContext.OrganizationId ?? Guid.Empty;
        personId = Guid.Empty;
        return organizationId != Guid.Empty
            && Guid.TryParse(User.FindFirstValue("person_id"), out personId)
            && personId != Guid.Empty;
    }

    private static TeacherPortalProfileResponse ToProfile(
        KiaiControl.Contracts.Teachers.TeacherResponse teacher) => new()
    {
        TeacherId = teacher.Id,
        FullName = teacher.FullName,
        PreferredName = teacher.PreferredName
    };
}
