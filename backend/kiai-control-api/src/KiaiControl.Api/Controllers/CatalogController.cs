using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController, Authorize, Route("api/v1/catalog")]
public sealed class CatalogController(OrganizationContext context, CatalogUseCase useCase, KiaiControl.UseCases.Teachers.TeacherUseCase teachers) : ControllerBase
{
    [HttpGet("clients"), Authorize(Roles = "owner,admin,reception", Policy = PermissionCodes.ClientsManage)]
    public Task<IActionResult> Clients(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default, string? area = null) => Run(() => useCase.ClientsAsync(Org, page, pageSize, search, ct, area));
    [HttpGet("teachers"), Authorize(Roles = "owner,admin", Policy = PermissionCodes.TeachersManage)]
    public Task<IActionResult> Teachers(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default, string? area = null) => Run(() => useCase.TeachersAsync(Org, page, pageSize, search, ct, area));
    [HttpGet("branches"), Authorize(Roles = "owner,admin,reception", Policy = PermissionCodes.BranchesManage)]
    public Task<IActionResult> Branches(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default) => Run(() => useCase.BranchesAsync(Org, page, pageSize, search, ct));
    [HttpGet("modalities"), Authorize(Roles = "owner,admin", Policy = PermissionCodes.ModalitiesManage)]
    public Task<IActionResult> Modalities(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default, string? area = null) => Run(() => useCase.ModalitiesAsync(Org, page, pageSize, search, ct, area));
    [HttpGet("lessons"), Authorize(Roles = "owner,admin,teacher,reception", Policy = PermissionCodes.LessonsManage)]
    public async Task<IActionResult> Lessons(int page = 1, int pageSize = 25, DateTimeOffset? from = null, DateTimeOffset? to = null, string? status = null, CancellationToken ct = default, string? area = null)
    {
        Guid? teacherId = null;
        if (User.IsInRole("teacher") && !User.IsInRole("owner") && !User.IsInRole("admin"))
        {
            if (!Guid.TryParse(User.FindFirst("person_id")?.Value, out var person))
                return Unauthorized();
            var teacher = await teachers.GetByPersonIdAsync(Org, person, ct);
            if (teacher is null || teacher.Status != "active")
                return NotFound();
            teacherId = teacher.Id;
        }

        return await Run(() => useCase.LessonsAsync(Org, page, pageSize, from, to, status, teacherId, ct, area));
    }

    [HttpGet("clients/branches"), Authorize(Roles = "owner,admin,reception", Policy = PermissionCodes.ClientsManage)]
    public Task<IActionResult> ClientBranches(int page = 1, int pageSize = 200, CancellationToken ct = default) => Run(() => useCase.BranchesAsync(Org, page, pageSize, null, ct));
    [HttpGet("teachers/branches"), Authorize(Roles = "owner,admin", Policy = PermissionCodes.TeachersManage)]
    public Task<IActionResult> TeacherBranches(int page = 1, int pageSize = 200, CancellationToken ct = default) => Run(() => useCase.BranchesAsync(Org, page, pageSize, null, ct));
    private Guid Org => context.OrganizationId ?? Guid.Empty;

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}
