using Microsoft.AspNetCore.Components;
using MudBlazor;
using KiaiControl.Admin.Services;

namespace KiaiControl.Admin.Components.Pages.Platform;

public abstract class PlatformPageBase : ComponentBase
{
    [Inject] protected AdminApiClient Api { get; set; } = default!;
    [Inject] protected AdminSession Session { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IDialogService Dialogs { get; set; } = default!;
    protected bool IsBusy { get; private set; }
    protected string? Error { get; private set; }
    protected abstract Task LoadAsync();

    protected override async Task OnParametersSetAsync()
    {
        if (!AdminNavigation.HasRole(Session.Current?.User, "platform_owner"))
        {
            Navigation.NavigateTo(AdminNavigation.Home(Session.Current?.User));
            return;
        }
        await RunAsync(LoadAsync);
    }

    protected async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        try { await action(); }
        catch (UnauthorizedAccessException)
        {
            await Session.ClearAsync();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException exception) { Error = exception.Message; }
        finally { IsBusy = false; }
    }
}
