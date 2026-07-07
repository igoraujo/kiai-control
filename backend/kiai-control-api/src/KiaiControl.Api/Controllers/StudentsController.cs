using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/students")]
public sealed class StudentsController(OrganizationContext organizationContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("students", "ready", organizationContext.OrganizationId));
    }
}
