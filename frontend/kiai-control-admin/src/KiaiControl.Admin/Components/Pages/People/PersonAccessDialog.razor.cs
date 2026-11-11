namespace KiaiControl.Admin.Components.Pages.People;
public partial class PersonAccessDialog
{
    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public DirectoryPersonResponse Person { get; set; } = default!;

    private IReadOnlyList<RoleResponse> _available = [];
    private List<string> _roles = [];
    private string _role = "reception";
    private string _email = "";
    private string _loginEmail = "";
    private string _password = "";
    private string? _error;
    private bool _busy;
    private bool IsOwner => AdminNavigation.HasRole(Session.Current?.User, "owner");
    private bool IsSelf => Person.PersonId == Session.Current?.User.PersonId;
    private bool Protected => IsSelf || (!IsOwner && _roles.Any(r => r is "owner" or "admin"));

    private bool Can(string p) => AdminNavigation.CanUse(Session.Current?.User, p);
    private IEnumerable<RoleResponse> AssignableRoles => _available.Where(r => (IsOwner || r.Code is not ("owner" or "admin")) && (r.Code != "client" || Person.ClientId is not null) && (r.Code != "teacher" || Person.TeacherId is not null));

    private bool CanEditRole(string r) => !Protected && (IsOwner || r is not ("owner" or "admin"));
    protected override async Task OnInitializedAsync()
    {
        _email = Person.Email ?? "";
        _loginEmail = Person.LoginEmail ?? _email;
        _roles = Person.Roles.ToList();
        try
        {
            _available = await Api.ListAccessRolesAsync();
        }
        catch (HttpRequestException)
        {
            _error = "Não foi possível carregar os perfis.";
        }
    }

    private void Close() => Dialog.Close(DialogResult.Ok(true));
    private async Task RunAsync(Func<Task> action)
    {
        _busy = true;
        _error = null;
        try
        {
            await action();
            _password = "";
            Snackbar.Add("Alteração salva.", Severity.Success);
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = e is UnauthorizedAccessException ? "Sua sessão expirou. Entre novamente." : e.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private Task AddProfileAsync(string role) => RunAsync(async () =>
    {
        await Api.AddProfileAsync(Person.PersonId, role);
        if (role == "client")
            Person.ClientId = Guid.Empty;
        else
            Person.TeacherId = Guid.Empty;
        _roles = (_roles.Append(role)).Distinct().ToList();
    });
    private Task CreateAsync()
    {
        if (!ValidatePassword())
            return Task.CompletedTask;
        return RunAsync(async () =>
        {
            await Api.CreateUserAccountAsync(Person.PersonId, new() { Email = _email, TemporaryPassword = _password, RoleCode = _role });
            Person.UserId = Guid.Empty;
            Person.LoginEmail = _email;
            _loginEmail = _email;
            Person.IsActive = true;
            _roles = (_roles.Append(_role)).Distinct().ToList();
        });
    }
    private Task AddRoleAsync() => RunAsync(async () =>
    {
        await Api.AssignPersonRoleAsync(Person.PersonId, _role);
        _roles = (_roles.Append(_role)).Distinct().ToList();
    });
    private Task RemoveRoleAsync(string role) => RunAsync(async () =>
    {
        await Api.RevokePersonRoleAsync(Person.PersonId, role);
        _roles.Remove(role);
    });
    private Task ChangeEmailAsync() => RunAsync(async () =>
    {
        await Api.SetAccountEmailAsync(Person.PersonId, _loginEmail);
        Person.LoginEmail = _loginEmail;
    });
    private Task ToggleAsync() => RunAsync(async () =>
    {
        await Api.SetAccountStateAsync(Person.PersonId, !Person.IsActive);
        Person.IsActive = !Person.IsActive;
    });
    private bool ValidatePassword()
    {
        if (PasswordPolicy.IsValid(_password))
            return true;
        _error = PasswordPolicy.Requirements;
        return false;
    }

    private Task ResetAsync() => ValidatePassword()
        ? RunAsync(() => Api.ResetAccountPasswordAsync(Person.PersonId, _password))
        : Task.CompletedTask;
}
