using KiaiControl.Contracts.Common;
using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiaiControl.Api.Controllers;

[ApiController]
[Route("api/v1/billing")]
public sealed class BillingController(OrganizationContext organizationContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<ContextStatusResponse> GetStatus()
    {
        return Ok(new ContextStatusResponse("billing", "ready", organizationContext.OrganizationId));
    }
}
