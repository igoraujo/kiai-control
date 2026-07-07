using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/teachers")]
public sealed class TeachersController(OrganizationContext organizationContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("teachers", "ready", organizationContext.OrganizationId));
    }
}
