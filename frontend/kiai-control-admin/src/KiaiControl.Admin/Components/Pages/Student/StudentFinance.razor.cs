namespace KiaiControl.Admin.Components.Pages.Student;
public partial class StudentFinance
{
    private static readonly System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
    private IReadOnlyList<KiaiControl.Contracts.Billing.BillingResponse> _invoices = [];
    private IReadOnlyList<KiaiControl.Contracts.Billing.PaymentResponse> _payments = [];
    private int _invoicePage = 1, _paymentPage = 1;
    private bool _busy;
    private string? _error;
    protected override Task OnInitializedAsync() => LoadAsync();
    private async Task InvoicePageAsync(int p)
    {
        _invoicePage = p;
        await LoadAsync();
    }

    private async Task PaymentPageAsync(int p)
    {
        _paymentPage = p;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            _invoices = await Api.PortalListAsync<KiaiControl.Contracts.Billing.BillingResponse>("invoices", _invoicePage);
            _payments = await Api.PortalListAsync<KiaiControl.Contracts.Billing.PaymentResponse>("payments", _paymentPage);
        }
        catch (Exception e)when (e is HttpRequestException or UnauthorizedAccessException)
        {
            _error = "Não foi possível carregar seu financeiro.";
        }
        finally
        {
            _busy = false;
        }
    }

    private static string Status(string s) => s switch
    {
        "paid" => "Paga",
        "partially_paid" => "Parcial",
        "overdue" => "Vencida",
        "canceled" => "Cancelada",
        "exempt" => "Isenta",
        _ => "Pendente"
    };
    private static string Method(string s) => s switch
    {
        "cash" => "Dinheiro",
        "pix" => "Pix",
        "credit_card" => "Crédito",
        "debit_card" => "Débito",
        "bank_transfer" => "Transferência",
        "boleto" => "Boleto",
        _ => "Ajuste manual"
    };
}
