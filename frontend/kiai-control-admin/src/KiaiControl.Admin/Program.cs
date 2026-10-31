using KiaiControl.Admin.Components;
using KiaiControl.Admin.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient("KiaiApi", client =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5210";
    client.BaseAddress = new Uri(apiBaseUrl.EndsWith('/') ? apiBaseUrl : $"{apiBaseUrl}/");
});
builder.Services.AddScoped<AdminSession>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<IAdminSessionStore, ProtectedBrowserSessionStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
