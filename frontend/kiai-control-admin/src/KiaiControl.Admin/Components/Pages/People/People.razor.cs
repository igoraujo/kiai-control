namespace KiaiControl.Admin.Components.Pages.People;
public partial class People
{
    private MudTable<DirectoryPersonResponse>? _table;
    private string _search = "";
    private string? _error;
    private async Task<TableData<DirectoryPersonResponse>> LoadAsync(TableState state, CancellationToken ct)
    {
        try
        {
            _error = null;
            var r = await Api.PeopleAsync(state.Page + 1, state.PageSize, _search);
            return new()
            {
                Items = r.Items,
                TotalItems = r.Total
            };
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar a equipe. Atualize a página ou entre novamente.";
            return new()
            {
                Items = [],
                TotalItems = 0
            };
        }
    }

    private async Task SearchAsync(string value)
    {
        _search = value;
        _table?.NavigateTo(Page.First);
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private async Task EditAsync(DirectoryPersonResponse? person)
    {
        var d = await Dialogs.ShowAsync<PersonDialog>(person is null ? "Nova pessoa da equipe" : "Editar pessoa", new DialogParameters<PersonDialog> { { x => x.Person, person } }, new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Small });
        if ((await d.Result)?.Canceled == false && _table is not null)
            await _table.ReloadServerData();
    }

    private async Task AccessAsync(DirectoryPersonResponse person)
    {
        var d = await Dialogs.ShowAsync<PersonAccessDialog>($"Acesso de {person.FullName}", new DialogParameters<PersonAccessDialog> { { x => x.Person, person } }, new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Medium });
        await d.Result;
        if (_table is not null)
            await _table.ReloadServerData();
    }
}
