namespace KiaiControl.Admin.Components.Pages.Teachers;
public partial class MyLessons
{
    private IReadOnlyList<TeacherPortalLessonResponse> _lessons = [];
    private string? _error;
    private bool _busy;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task LoadAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            _lessons = await Api.GetAsync<List<TeacherPortalLessonResponse>>("mobile/teacher/lessons");
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar sua agenda. Confira seu vínculo de professor com a academia.";
        }
        finally
        {
            _busy = false;
        }
    }
}
