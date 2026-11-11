using KiaiControl.Api.Middlewares;
using KiaiControl.Api.Services;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data;

namespace KiaiControl.UnitTests;

public sealed class PlatformPrivacyAndTelemetryTests
{
    [Fact]
    public void AuditMetadata_ShouldUseAllowlistAndRemoveCredentialsContactsAndHealthDataAtAnyDepth()
    {
        var safe = PlatformAuditPrivacy.SafeMetadata("""
            {"password":"never-return","token":"secret-token","after":{
                "Email":"private@example.test","FullName":"Private person","Document":"12345678900",
                "healthData":{"diagnosis":"private"},"PasswordHash":"hashed-secret","IsActive":false,"Roles":["owner"]},
                "context":{"reason":"Investigação de bloqueio","filters":{"ActorSearch":"private@example.test","organizationId":"abc"}}}
            """);
        foreach (var secret in new[] { "never-return", "secret-token", "private@example.test", "Private person", "12345678900", "diagnosis", "hashed-secret" })
            Assert.DoesNotContain(secret, safe);
        Assert.Contains("IsActive", safe);
        Assert.Contains("owner", safe);
        Assert.Equal("{}", PlatformAuditPrivacy.SafeMetadata("not-json"));
    }

    [Fact]
    public void TelemetryBuffer_ShouldAggregateConcurrentRequestsWithoutKeepingIndividualEvents()
    {
        var buffer = new PlatformTelemetryBuffer();
        var bucket = DateTimeOffset.UtcNow;
        Parallel.For(0, 1000, _ => buffer.Add(new() { BucketStart = bucket, Route = "api/v1/clients/{id}", Method = "GET", StatusCode = 200, Requests = 1, DurationTotalMs = 25, MaxDurationMs = 25 }));
        var aggregate = Assert.Single(buffer.Drain());
        Assert.Equal(1000, aggregate.Requests);
        Assert.Equal(25000, aggregate.DurationTotalMs);
        Assert.Equal(25, aggregate.MaxDurationMs);
        Assert.Empty(buffer.Drain());
    }

    [Fact]
    public async Task TelemetryMiddleware_ShouldKeepOnlyRouteTemplateAndIgnoreSpoofedTenantHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/clients/secret-person";
        context.Request.QueryString = new("?email=private@example.test&token=secret-token");
        context.Request.Headers["X-Organization-Id"] = Guid.NewGuid().ToString();
        context.Request.Method = "GET";
        context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("api/v1/clients/{id}"), 0, EndpointMetadataCollection.Empty, "test"));
        var buffer = new PlatformTelemetryBuffer();
        await new PlatformTelemetryMiddleware(_ => Task.CompletedTask).InvokeAsync(context, buffer);
        var aggregate = Assert.Single(buffer.Drain());
        Assert.Equal("api/v1/clients/{id}", aggregate.Route);
        Assert.Null(aggregate.OrganizationId);
        Assert.Equal(1, aggregate.Requests);
    }

    [Fact]
    public async Task DatabaseHealth_ShouldReportFailureWithoutExposingConnectionDetails()
    {
        var result = await new PlatformDatabaseHealthCheck(new FailingConnection()).CheckHealthAsync(new());
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Dependência indisponível.", result.Description);
        Assert.Null(result.Exception);
    }

    private sealed class FailingConnection : IDbConnectionFactory
    {
        public Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Host=private;Password=never-expose");
    }
}
