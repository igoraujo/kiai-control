using KiaiControl.Admin.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace KiaiControl.Admin.Components.Common;

public abstract class CrudPageBase<T> : ComponentBase
{
    [Inject] protected AdminSession Session { get; set; } = default!;
    [Inject] protected AdminApiClient Api { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    protected abstract string Resource { get; }

    protected List<T> Items { get; private set; } = [];
    protected bool IsLoading { get; private set; } = true;
    protected string? ErrorMessage { get; private set; }
    protected string Search { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (Session.Current is null)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        await LoadExtrasAsync();
        await LoadAsync();
    }

    protected virtual Task LoadExtrasAsync() => Task.CompletedTask;

    protected async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Items = [.. await Api.ListAsync<T>(Resource)];
        }
        catch (UnauthorizedAccessException)
        {
            Session.Clear();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "Você não tem permissão para acessar este recurso."
                : $"Não foi possível carregar os dados. {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task<bool> OpenDialogAsync<TDialog>(string title, DialogParameters<TDialog> parameters)
        where TDialog : ComponentBase
    {
        var dialog = await DialogService.ShowAsync<TDialog>(title, parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true });
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            Snackbar.Add("Registro salvo com sucesso.", Severity.Success);
            await LoadAsync();
            return true;
        }

        return false;
    }

    protected async Task DeleteAsync(Guid id, string name)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Confirmar exclusão", $"Deseja excluir \"{name}\"?", yesText: "Excluir", cancelText: "Cancelar");
        if (confirmed != true)
        {
            return;
        }

        try
        {
            await Api.DeleteAsync(Resource, id);
            Snackbar.Add("Registro excluído.", Severity.Success);
            await LoadAsync();
        }
        catch (UnauthorizedAccessException)
        {
            Session.Clear();
            Navigation.NavigateTo("/login");
        }
        catch (HttpRequestException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
        }
    }
}
