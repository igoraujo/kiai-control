using System.Security.Claims;
using KiaiControl.Contracts.Administration;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;
[ApiController, Authorize(Roles = "owner,admin", Policy = PermissionCodes.AccessManage), Route("api/v1/people")]
public sealed class PeopleController(OrganizationContext context, AdministrationUseCase useCase) : ControllerBase
{
    private Guid Org => context.OrganizationId ?? Guid.Empty;
    private Guid Actor => Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet]
    public Task<IActionResult> List(int page = 1, int pageSize = 25, string? search = null, CancellationToken ct = default) => Run(async () => Ok(await useCase.ListPeopleAsync(Org, page, pageSize, search, ct)));
    [HttpPost]
    public Task<IActionResult> Create(SavePersonRequest request, CancellationToken ct) => Run(async () => Ok(new { PersonId = await useCase.SavePersonAsync(Org, null, request, ct) }));
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, SavePersonRequest request, CancellationToken ct) => Run(async () => await useCase.SavePersonAsync(Org, id, request, ct) == Guid.Empty ? NotFound() : NoContent());
    [HttpPost("{id:guid}/profiles/{profile}")]
    public Task<IActionResult> AddProfile(Guid id, string profile, CancellationToken ct) => Run(async () =>
    {
        var permission = profile == "client" ? PermissionCodes.ClientsManage : PermissionCodes.TeachersManage;
        if (!User.HasClaim(PermissionCodes.ClaimType, permission))
            return Forbid();
        return await useCase.AddProfileAsync(Org, id, profile, Actor, ct) ? NoContent() : NotFound();
    });
    [HttpPut("{id:guid}/account/state")]
    public Task<IActionResult> State(Guid id, AccountStateRequest request, CancellationToken ct) => Manage(id, request.IsActive, null, ct);
    [HttpPut("{id:guid}/account/password")]
    public Task<IActionResult> Password(Guid id, TemporaryPasswordRequest request, CancellationToken ct) => Manage(id, null, request.Password, ct);
    [HttpPost("{id:guid}/account/revoke-sessions")]
    public Task<IActionResult> Revoke(Guid id, CancellationToken ct) => Manage(id, null, null, ct);
    [HttpPut("{id:guid}/account/email")]
    public Task<IActionResult> Email(Guid id, AccountEmailRequest request, CancellationToken ct) => Run(async () =>
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Informe o novo e-mail de acesso.");
        return await useCase.ManageAccountAsync(Org, id, Actor, User.IsInRole("owner"), null, null, ct, request.Email) ? NoContent() : Conflict("A conta não está disponível para esta alteração.");
    });
    private Task<IActionResult> Manage(Guid id, bool? active, string? password, CancellationToken ct) => Run(async () => await useCase.ManageAccountAsync(Org, id, Actor, User.IsInRole("owner"), active, password, ct) ? NoContent() : Conflict("A conta não está disponível para esta ação. Você não pode alterar a própria conta nem uma conta de nível superior."));
    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (KiaiControl.Core.Common.ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
}
