namespace KiaiControl.Admin.Components.Pages.People;
public partial class PersonDialog
{
    [Parameter]
    public DirectoryPersonResponse? Person { get; set; }

    private SavePersonRequest _form = new();
    protected override void OnInitialized()
    {
        if (Person is not null)
            _form = new()
            {
                FullName = Person.FullName,
                Email = Person.Email,
                Phone = Person.Phone
            };
    }

    protected override Task SaveCoreAsync() => Api.SavePersonAsync(Person?.PersonId, _form);
}
