using System.Security.Claims;
using KiaiControl.Contracts.Notifications;
using KiaiControl.Core.Common;
using KiaiControl.UseCases.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    OrganizationContext organizationContext,
    NotificationUseCase notificationUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "owner,admin,teacher,reception")]
    public async Task<ActionResult<NotificationResponse>> CreateAsync(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationContext.OrganizationId is not Guid organizationId)
        {
            return BadRequest("Organization context is required.");
        }

        try
        {
            var created = await notificationUseCase.CreateAsync(organizationId, request, cancellationToken);
            return created is null
                ? NotFound("The notification recipient does not exist in this organization.")
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