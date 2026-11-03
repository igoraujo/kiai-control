using KiaiControl.Api.Extensions;
using KiaiControl.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKiaiApi(builder.Configuration, builder.Environment);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<PlatformOwnerBootstrapper>()
        .EnsureProvisionedAsync();
}

app.UseKiaiApi();

app.Run();

public partial class Program;
