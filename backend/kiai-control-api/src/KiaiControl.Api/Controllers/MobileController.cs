using System.Security.Claims;
using KiaiControl.Contracts.Mobile;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Auth;
using KiaiControl.UseCases.Mobile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController, Authorize, Route("api/v1/mobile")]
public sealed class MobileController(OrganizationContext context, MobileUseCase mobile, AuthUseCase auth) : ControllerBase
{
    [HttpGet("context")]
    public async Task<IActionResult> Context(CancellationToken ct)
    {
        if (!Ids(out var org, out var person) || !Guid.TryParse(User.FindFirstValue("sub"), out var id)) return Unauthorized();
        var user = await auth.GetMeAsync(id, ct);
        if (user is null) return Unauthorized();
        var result = await mobile.ContextAsync(org, person, user, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("account")]
    public async Task<IActionResult> Account(CancellationToken ct)
    {
        if (!Ids(out var org, out var person)) return Unauthorized();
        var result = await mobile.AccountAsync(org, person, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("account")]
    public Task<IActionResult> SaveAccount(SaveMobileAccountRequest r, CancellationToken ct) => Mutate((o, p) => mobile.SaveAccountAsync(o, p, r, ct));

    [HttpPut("account/photo"), RequestSizeLimit(750000)]
    public Task<IActionResult> SavePhoto(SaveMobilePhotoRequest r, CancellationToken ct) => Mutate((o, p) => mobile.SavePhotoAsync(o, p, r, ct));

    [HttpDelete("account/photo")]
    public Task<IActionResult> RemovePhoto(CancellationToken ct) => Mutate((o, p) => mobile.RemovePhotoAsync(o, p, ct));

    [Authorize(Roles = "client"), HttpGet("progress")]
    public Task<IActionResult> Progress(CancellationToken ct) => ForClient(async (org, id) => Ok(await mobile.ProgressAsync(org, id, ct)), ct);

    [Authorize(Roles = "client"), HttpGet("rankings")]
    public Task<IActionResult> Rankings(bool yearOnly = true, int page = 1, int pageSize = 25, CancellationToken ct = default) =>
        ForClient(async (org, id) => Ok(await mobile.RankingAsync(org, id, yearOnly, page, pageSize, ct)), ct);

    [HttpPut("devices")]
    public Task<IActionResult> RegisterDevice(RegisterDeviceRequest r, CancellationToken ct) => Mutate((o, p) => mobile.RegisterDeviceAsync(o, p, r, ct));

    [HttpDelete("devices/{installation}")]
    public async Task<IActionResult> RemoveDevice(string installation, CancellationToken ct)
    {
        if (!Ids(out var org, out var person)) return Unauthorized();
        await mobile.RemoveDeviceAsync(org, person, installation, ct);
        return NoContent();
    }

    private async Task<IActionResult> ForClient(Func<Guid, Guid, Task<IActionResult>> action, CancellationToken ct)
    {
        if (!Ids(out var org, out var person)) return Unauthorized();
        var client = await mobile.ClientAsync(org, person, ct);
        if (client is null) return NotFound();
        try { return await action(org, client.Value); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }

    private async Task<IActionResult> Mutate(Func<Guid, Guid, Task<bool>> action)
    {
        if (!Ids(out var org, out var person)) return Unauthorized();
        try { return await action(org, person) ? NoContent() : NotFound(); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }

    private bool Ids(out Guid org, out Guid person)
    {
        org = context.OrganizationId ?? Guid.Empty;
        return Guid.TryParse(User.FindFirstValue("person_id"), out person) && org != Guid.Empty && person != Guid.Empty;
    }
}
