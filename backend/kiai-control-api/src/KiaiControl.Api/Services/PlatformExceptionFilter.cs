using KiaiControl.Core.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KiaiControl.Api.Services;

public sealed class PlatformExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            ArgumentException => 400,
            KeyNotFoundException => 404,
            ConflictException => 409,
            _ => 0
        };
        if (status == 0) return;
        context.Result = new ObjectResult(context.Exception.Message) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
