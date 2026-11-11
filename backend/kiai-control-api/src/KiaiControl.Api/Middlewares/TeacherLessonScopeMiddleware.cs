using System.Security.Claims;
using KiaiControl.Core.Common;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Api.Middlewares;
/// <summary>Applies the same teacher ownership rule to web and mobile attendance operations.</summary>
public sealed class TeacherLessonScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, OrganizationContext context, ITeacherRepository teachers, ILessonRepository lessons, IAttendanceRepository attendance)
    {
        var teacherOnly = http.User.IsInRole("teacher") && !http.User.IsInRole("owner") && !http.User.IsInRole("admin") && !http.User.IsInRole("reception");
        var segments = http.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (!teacherOnly || segments.Length < 3 || segments[0] != "api" || segments[1] != "v1" || segments[2] is not ("lessons" or "attendance"))
        {
            await next(http);
            return;
        }

        Guid? lessonId = null;
        if (segments.Length > 3 && Guid.TryParse(segments[3], out var target))
        {
            if (segments[2] == "lessons")
                lessonId = target;
            else if (context.OrganizationId is Guid org)
                lessonId = (await attendance.GetByIdAsync(org, target, http.RequestAborted))?.LessonId;
        }
        else if (segments[2] == "attendance" && (segments.Length == 3 || segments[3] != "pending" && segments[3] != "check-in"))
        {
            http.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (segments.Length > 3 && Guid.TryParse(segments[3], out _))
        {
            if (lessonId is null || context.OrganizationId is not Guid org || !Guid.TryParse(http.User.FindFirstValue("person_id"), out var person))
            {
                http.Response.StatusCode = 404;
                return;
            }

            var teacher = await teachers.GetByPersonIdAsync(org, person, http.RequestAborted);
            if (teacher is null || teacher.Status != "active" || await lessons.GetByIdForTeacherAsync(org, lessonId.Value, teacher.Id, http.RequestAborted)is null)
            {
                http.Response.StatusCode = 404;
                return;
            }
        }

        await next(http);
    }
}
