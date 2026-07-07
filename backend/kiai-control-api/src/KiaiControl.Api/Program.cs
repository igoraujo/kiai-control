using KiaiControl.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKiaiApi(builder.Configuration);

var app = builder.Build();

app.UseKiaiApi();

app.Run();

public partial class Program;
