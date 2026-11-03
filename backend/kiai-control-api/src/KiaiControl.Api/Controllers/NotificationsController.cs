using System.Security.Claims;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Notifications;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize]
[Authorize(Policy = PermissionCodes.NotificationsRead)]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    OrganizationContext organizationContext,
    NotificationUseCase notificationUseCase,
    ClientUseCase clientUseCase) : ControllerBase
{
    [HttpGet("options")]
    [Authorize(Policy = PermissionCodes.NotificationsManage)]
    public async Task<ActionResult<NotificationOptionsResponse>> OptionsAsync(CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        var clients = await clientUseCase.ListAsync(organizationId, 1, 200, cancellationToken);
        return Ok(new NotificationOptionsResponse
        {
            Clients = clients.Where(client => client.Status == "active").ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    [Authorize(Policy = PermissionCodes.NotificationsManage)]
    public async Task<ActionResult<NotificationResponse>> CreateAsync(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        try
        {
            var created = await notificationUseCase.CreateAsync(organizationId, request, cancellationToken);
            return created is null
                ? NotFound(SystemMessages.TheNotificationRecipientDoesNotExistInThisOrganization)
                : StatusCode(StatusCodes.Status201Created, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> ListMineAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var personId))
        {
            return Unauthorized();
        }

        var items = await notificationUseCase.ListMineAsync(organizationId, personId, page, pageSize, cancellationToken);
        return Ok(items);
    }

    [HttpGet("outbox")]
    [Authorize(Policy = PermissionCodes.NotificationsManage)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> ListOrganizationAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest(SystemMessages.OrganizationContextIsRequired);
        }

        return Ok(await notificationUseCase.ListOrganizationAsync(organizationId, page, pageSize, cancellationToken));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkMineAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId || !TryGetPersonId(out var personId))
        {
            return Unauthorized();
        }

        var updated = await notificationUseCase.MarkMineAsReadAsync(organizationId, personId, id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    private bool TryGetPersonId(out Guid personId)
    {
        return Guid.TryParse(User.FindFirstValue("person_id"), out personId);
    }
}