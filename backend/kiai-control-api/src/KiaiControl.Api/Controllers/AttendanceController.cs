using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
public sealed class AttendanceController(OrganizationContext organizationContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("attendance", "ready", organizationContext.OrganizationId));
    }
}
