using Bunit;
using KiaiControl.Admin.Components.Common;
using KiaiControl.Contracts.Branches;
using MudBlazor;
using MudBlazor.Services;

namespace KiaiControl.Admin.UnitTests;

public sealed class PersonFormFieldsTests
{
    [Fact]
    public async Task PersonFormFields_ShouldRejectEmptyFullName()
    {
        await using var context = CreateContext();
        var form = context.Render<MudForm>(parameters => parameters.AddChildContent<PersonFormFields>(child => child
            .Add(component => component.Model, new PersonFormModel())
            .Add(component => component.Branches, Array.Empty<BranchResponse>())));

        await form.InvokeAsync(() => form.Instance.ValidateAsync());

        Assert.False(form.Instance.IsValid);
    }

    [Fact]
    public async Task PersonFormFields_ShouldAcceptProvidedFullName()
    {
        await using var context = CreateContext();
        var form = context.Render<MudForm>(parameters => parameters.AddChildContent<PersonFormFields>(child => child
            .Add(component => component.Model, new PersonFormModel { FullName = "Ana Silva" })
            .Add(component => component.Branches, Array.Empty<BranchResponse>())));

        await form.InvokeAsync(() => form.Instance.ValidateAsync());

        Assert.True(form.Instance.IsValid);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddMudServices();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }
}