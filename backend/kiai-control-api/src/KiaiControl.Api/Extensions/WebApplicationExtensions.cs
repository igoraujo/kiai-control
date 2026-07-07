using KiaiControl.Api.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace KiaiControl.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseKiaiApi(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<OrganizationContextMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
