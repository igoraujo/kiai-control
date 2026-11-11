namespace KiaiControl.Admin.Components.Pages.Student;
public partial class Student
{
    private ClientPortalProfileResponse? _profile;
    private string? _error;
    protected override async Task OnInitializedAsync()
    {
        Session.SelectArea("shared");
        try
        {
            _profile = await Api.GetAsync<ClientPortalProfileResponse>("portal/profile");
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Seu cadastro de aluno não está ativo ou não pôde ser carregado. Fale com a academia.";
        }
    }
}
