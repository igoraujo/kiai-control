using KiaiControl.Admin.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace KiaiControl.Admin.Components.Common;

public abstract class FormDialogBase : ComponentBase
{
    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] protected AdminApiClient Api { get; set; } = default!;

    protected MudForm? Form { get; set; }
    protected bool IsSaving { get; private set; }
    protected string? ErrorMessage { get; private set; }

    protected abstract Task SaveCoreAsync();

    protected void Cancel() => MudDialog.Cancel();

    protected async Task SubmitAsync()
    {
        if (Form is not null)
        {
            await Form.ValidateAsync();
            if (!Form.IsValid)
            {
                return;
            }
        }

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            await SaveCoreAsync();
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Sessão expirada. Faça login novamente.";
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
