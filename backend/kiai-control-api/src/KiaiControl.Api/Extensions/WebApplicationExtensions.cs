using KiaiControl.Api.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace KiaiControl.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseKiaiApi(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<OrganizationContextMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<MutationAuditMiddleware>();

        app.MapControllers();
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
