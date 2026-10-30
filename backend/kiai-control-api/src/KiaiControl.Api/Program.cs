using KiaiControl.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKiaiApi(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseKiaiApi();

app.Run();

public partial class Program;
