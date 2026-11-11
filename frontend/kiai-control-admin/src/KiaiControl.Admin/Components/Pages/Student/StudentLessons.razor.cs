namespace KiaiControl.Admin.Components.Pages.Student;
public partial class StudentLessons
{
    private IReadOnlyList<ClientPortalLessonResponse> _lessons = [];
    private bool _busy;
    private string? _error;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            _lessons = await Api.PortalListAsync<ClientPortalLessonResponse>("lessons");
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar sua agenda. Tente novamente.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CheckInAsync(Guid id)
    {
        try
        {
            _busy = true;
            await Api.CheckInAsync(id);
            Snackbar.Add("Check-in registrado.", Severity.Success);
            await LoadAsync();
        }
        catch (HttpRequestException e)
        {
            _error = e.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CancelAsync(Guid id)
    {
        try
        {
            _busy = true;
            await Api.DeleteAsync($"attendance/check-in/{id}", queryString: null);
            await LoadAsync();
            Snackbar.Add("Check-in cancelado.", Severity.Success);
        }
        catch (HttpRequestException e)
        {
            _error = e.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
