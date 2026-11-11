using KiaiControl.Admin.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using KiaiControl.Contracts.Common;

namespace KiaiControl.Admin.Components.Common;
public abstract class CrudPageBase<T> : ComponentBase
{
    [Inject]
    protected AdminSession Session { get; set; } = default!;

    [Inject]
    protected AdminApiClient Api { get; set; } = default!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = default!;
    protected abstract string Resource { get; }
    protected List<T> Items { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;
    protected string? ErrorMessage { get; private set; }
    protected string Search { get; set; } = string.Empty;
    protected bool CanManage => AdminNavigation.CanUse(Session.Current?.User, Resource + ".manage");

    [SupplyParameterFromQuery(Name = "area")]
    public string? AreaQuery { get; set; }
    protected string SelectedArea { get; private set; } = AcademyArea.General;
    private bool _areaInitialized;
    private bool UsesArea => Resource is "clients" or "teachers" or "modalities" or "lessons";

    protected override async Task OnParametersSetAsync()
    {
        if (!UsesArea)
            return;
        string area;
        try { area = AcademyArea.Normalize(AreaQuery); }
        catch (ArgumentException) { area = AcademyArea.General; }
        var changed = _areaInitialized && area != SelectedArea;
        SelectedArea = area;
        _areaInitialized = true;
        Session.SelectArea(area);
        if (changed && Table is not null)
        {
            Table.NavigateTo(Page.First);
            await LoadAsync();
        }
    }

    protected MudTable<T>? Table;
    protected override async Task OnInitializedAsync()
    {
        if (Session.Current is null)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        try
        {
            await LoadExtrasAsync();
        }
        catch (UnauthorizedAccessException)
        {
            await Session.ClearAsync();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException e)
        {
            ErrorMessage = e.StatusCode == System.Net.HttpStatusCode.Forbidden ? AdminSystemMessages.ResourceAccessPermissionDenied : "Não foi possível carregar as opções do cadastro. Tente novamente.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected virtual Task LoadExtrasAsync() => Task.CompletedTask;
    protected async Task LoadAsync()
    {
        if (Resource == "modalities")
        {
            try { Session.SetCapabilities(await Api.GetWorkspaceCapabilitiesAsync()); }
            catch(HttpRequestException){ ErrorMessage="Não foi possível atualizar as áreas da academia. Tente novamente."; }
        }
        if (Table is not null)
            await Table.ReloadServerData();
    }

    protected async Task SearchAsync(string value)
    {
        Search = value;
        Table?.NavigateTo(Page.First);
        await LoadAsync();
    }

    protected virtual async Task<TableData<T>> LoadServerData(TableState state, CancellationToken ct)
    {
        try
        {
            ErrorMessage = null;
            var result = await Api.CatalogAsync<T>(Resource, state.Page + 1, state.PageSize, Search, UsesArea ? SelectedArea : null);
            Items = [..result.Items];
            return new TableData<T>
            {
                Items = result.Items,
                TotalItems = result.Total
            };
        }
        catch (UnauthorizedAccessException)
        {
            await Session.ClearAsync();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException e)
        {
            ErrorMessage = e.StatusCode == System.Net.HttpStatusCode.Forbidden ? AdminSystemMessages.ResourceAccessPermissionDenied : "Não foi possível carregar os registros. Tente novamente.";
        }

        return new TableData<T>
        {
            Items = [],
            TotalItems = 0
        };
    }

    protected async Task<bool> OpenDialogAsync<TDialog>(string title, DialogParameters<TDialog> parameters, MaxWidth maxWidth = MaxWidth.Small)
        where TDialog : ComponentBase
    {
        var dialog = await DialogService.ShowAsync<TDialog>(title, parameters, new DialogOptions { MaxWidth = maxWidth, FullWidth = true, CloseOnEscapeKey = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            Snackbar.Add(AdminSystemMessages.RecordSavedSuccessfully, Severity.Success);
            await LoadAsync();
            return true;
        }

        return false;
    }

    protected async Task DeleteAsync(Guid id, string name)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync("Confirmar exclusão", $"Deseja excluir \"{name}\"?", yesText: "Excluir", cancelText: "Cancelar");
        if (confirmed != true)
        {
            return;
        }

        try
        {
            await Api.DeleteAsync(Resource, id);
            Snackbar.Add(AdminSystemMessages.RecordDeleted, Severity.Success);
            await LoadAsync();
        }
        catch (UnauthorizedAccessException)
        {
            await Session.ClearAsync();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}
