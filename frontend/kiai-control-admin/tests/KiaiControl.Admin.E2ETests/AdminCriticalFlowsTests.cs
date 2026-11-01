using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using KiaiControl.Services.Auth;
using Microsoft.Playwright;
using Npgsql;
using Testcontainers.PostgreSql;

namespace KiaiControl.Admin.E2ETests;

public sealed class AdminCriticalFlowsTests
{
    [Fact]
    public async Task AdminCanManageClientAttendanceAndPaymentInBrowser()
    {
        await using var environment = new E2EEnvironment();
        await environment.StartAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{environment.AdminUrl}/login");
        await page.GetByLabel("E-mail").FillAsync(environment.OwnerEmail);
        await page.GetByLabel("Senha").FillAsync(environment.OwnerPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();
        try
        {
            await page.WaitForFunctionAsync("() => window.location.pathname === '/'", options: new() { Timeout = 10_000 });
        }
        catch (TimeoutException exception)
        {
            var body = await page.Locator("body").InnerTextAsync();
            throw new InvalidOperationException(
                $"Admin login did not complete. URL: {page.Url}{Environment.NewLine}Page: {body}{Environment.NewLine}{environment.ProcessOutput}",
                exception);
        }

        const string modalityName = "Modalidade E2E";
        const string clientName = "Aluno E2E";
        var originalEmail = environment.ClientEmail;
        var updatedEmail = environment.UpdatedClientEmail;

        await page.GotoAsync($"{environment.AdminUrl}/modalities");
        await page.GetByRole(AriaRole.Button, new() { Name = "Nova modalidade" }).ClickAsync();
        await page.GetByLabel("Nome", new() { Exact = true }).FillAsync(modalityName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await page.GetByText(modalityName, new() { Exact = true }).WaitForAsync();

        await page.GotoAsync($"{environment.AdminUrl}/clients");
        await page.GetByRole(AriaRole.Button, new() { Name = "Novo aluno" }).ClickAsync();
        await page.GetByLabel("Nome completo").FillAsync(clientName);
        await page.GetByLabel("Email", new() { Exact = true }).FillAsync(originalEmail);
        await page.GetByLabel("Telefone", new() { Exact = true }).FillAsync("(11) 99999-0101");
        await page.GetByLabel("Modalidades do aluno").ClickAsync();
        await page.GetByText(modalityName, new() { Exact = true }).Last.ClickAsync();
        await page.GetByLabel("Modalidades do aluno").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await page.GetByText(clientName, new() { Exact = true }).WaitForAsync();

        var clientRow = page.GetByRole(AriaRole.Row).Filter(new() { HasText = clientName });
        await clientRow.GetByRole(AriaRole.Button, new() { Name = "Editar" }).ClickAsync();
        await page.GetByLabel("Email", new() { Exact = true }).FillAsync(updatedEmail);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await page.GetByText(updatedEmail, new() { Exact = true }).WaitForAsync();

        var client = await environment.GetClientAsync(updatedEmail);
        var lessonId = await CreateLessonAsync(page, environment, modalityName);
        await environment.CreateClientLoginAsync(client.PersonId, updatedEmail);
        await environment.EnableClientCheckInAsync();

        var checkIn = await environment.CheckInAsync(updatedEmail, lessonId);
        Assert.Equal("pending_confirmation", checkIn.Status);

        await page.GotoAsync($"{environment.AdminUrl}/lessons/{lessonId}/attendance");
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = clientName }).First.WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar presença" }).ClickAsync();
        await page.GetByText("Nenhum check-in aguardando validação.").WaitForAsync();
        Assert.Equal(1, await environment.CountValidatedAttendanceAsync(lessonId));

        await page.GotoAsync($"{environment.AdminUrl}/billing");
        await page.GetByRole(AriaRole.Tab, new() { Name = "Planos" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Novo plano" }).ClickAsync();
        await page.GetByLabel("Nome do plano").FillAsync("Plano E2E");
        await page.GetByLabel("Valor mensal").FillAsync("150");
        await page.GetByLabel("Dia do vencimento").FillAsync("10");
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar plano" }).ClickAsync();
        await page.GetByText("Plano E2E", new() { Exact = true }).WaitForAsync();

        await page.GetByRole(AriaRole.Tab, new() { Name = "Assinaturas" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Nova assinatura" }).ClickAsync();
        await page.GetByLabel("Aluno").ClickAsync();
        await page.GetByText(clientName, new() { Exact = true }).Last.ClickAsync();
        await page.GetByLabel("Plano").ClickAsync();
        await page.GetByText("Plano E2E", new() { Exact = false }).Last.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Assinar plano" }).ClickAsync();
        await page.GetByText(clientName, new() { Exact = true }).WaitForAsync();

        await page.GetByRole(AriaRole.Tab, new() { Name = "Faturas" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Gerar faturas" }).ClickAsync();
        var invoiceRow = page.GetByRole(AriaRole.Row).Filter(new() { HasText = clientName });
        await invoiceRow.GetByRole(AriaRole.Button, new() { Name = "Registrar pagamento" }).WaitForAsync();
        await invoiceRow.GetByRole(AriaRole.Button, new() { Name = "Registrar pagamento" }).ClickAsync();
        var paymentDialog = page.GetByRole(AriaRole.Dialog);
        await paymentDialog.GetByRole(AriaRole.Button, new() { Name = "Registrar pagamento" }).ClickAsync();
        await invoiceRow.GetByText("Paga", new() { Exact = true }).WaitForAsync();

        await page.GetByRole(AriaRole.Tab, new() { Name = "Pagamentos" }).ClickAsync();
        await page.GetByText(clientName, new() { Exact = true }).WaitForAsync();
        Assert.Equal(1, await environment.CountPaymentsAsync(client.ClientId));
    }

    private static async Task<Guid> CreateLessonAsync(IPage page, E2EEnvironment environment, string modalityName)
    {
        await page.GotoAsync($"{environment.AdminUrl}/modalities");
        var modalityRow = page.GetByRole(AriaRole.Row).Filter(new() { HasText = modalityName });
        await modalityRow.GetByRole(AriaRole.Button, new() { Name = "Gerenciar horários da modalidade" }).ClickAsync();

        await page.GetByLabel("Início").FillAsync("00:00");
        await page.GetByLabel("Término").FillAsync("23:59");
        await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar horário" }).ClickAsync();
        await page.GetByText("Horário adicionado com sucesso.").WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Gerar agenda" }).ClickAsync();

        var lessons = await environment.WaitForGeneratedLessonsAsync(page);
        Assert.NotEmpty(lessons);
        return lessons[0];
    }
}

internal sealed class E2EEnvironment : IAsyncDisposable
{
    private const string OwnerPasswordValue = "Owner!E2E123456";
    private const string ClientPassword = "Client!E2E123456";
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("kiai_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    private readonly ConcurrentQueue<string> _apiOutput = new();
    private readonly ConcurrentQueue<string> _adminOutput = new();
    private Process? _apiProcess;
    private Process? _adminProcess;
    private int _apiPort;
    private int _adminPort;
    private Guid _organizationId;

    public string OwnerEmail { get; } = $"owner-{Guid.NewGuid():N}@example.test";
    public string OwnerPassword => OwnerPasswordValue;
    public string ClientEmail { get; } = $"client-{Guid.NewGuid():N}@example.test";
    public string UpdatedClientEmail { get; } = $"updated-client-{Guid.NewGuid():N}@example.test";
    public string ApiUrl => $"http://127.0.0.1:{_apiPort}";
    public string AdminUrl => $"http://127.0.0.1:{_adminPort}";
    public string ProcessOutput => $"API:{Environment.NewLine}{FormatOutput(_apiOutput)}{Environment.NewLine}Admin:{Environment.NewLine}{FormatOutput(_adminOutput)}";
    private string ConnectionString => _database.GetConnectionString();

    public async Task StartAsync()
    {
        await _database.StartAsync();
        await ApplyMigrationsAsync();

        var root = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        _apiPort = GetAvailablePort();
        _adminPort = GetAvailablePort();
        _apiProcess = StartApplication(
            Path.Combine(root, "backend", "kiai-control-api", "src", "KiaiControl.Api", "bin", configuration, "net10.0", "KiaiControl.Api.dll"),
            Path.Combine(root, "backend", "kiai-control-api", "src", "KiaiControl.Api"),
            _apiOutput,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = ApiUrl,
                ["TZ"] = "UTC",
                ["ConnectionStrings__PostgreSql"] = ConnectionString,
                ["Cache__Provider"] = "Memory"
            });
        await WaitForHttpAsync($"{ApiUrl}/health/live", _apiProcess, _apiOutput);

        using var httpClient = new HttpClient();
        var registrationResponse = await httpClient.PostAsJsonAsync($"{ApiUrl}/api/v1/organizations", new
        {
            name = $"E2E Academy {Guid.NewGuid():N}",
            ownerFullName = "E2E Owner",
            ownerEmail = OwnerEmail,
            ownerPassword = OwnerPassword
        });
        registrationResponse.EnsureSuccessStatusCode();
        using (var registration = JsonDocument.Parse(await registrationResponse.Content.ReadAsStringAsync()))
        {
            _organizationId = registration.RootElement.GetProperty("organization").GetProperty("id").GetGuid();
        }

        await SetOrganizationTimezoneAsync();
        _adminProcess = StartApplication(
            Path.Combine(root, "frontend", "kiai-control-admin", "src", "KiaiControl.Admin", "bin", configuration, "net10.0", "KiaiControl.Admin.dll"),
            Path.Combine(root, "frontend", "kiai-control-admin", "src", "KiaiControl.Admin"),
            _adminOutput,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = AdminUrl,
                ["TZ"] = "UTC",
                ["Api__BaseUrl"] = ApiUrl
            });
        await WaitForHttpAsync(AdminUrl, _adminProcess, _adminOutput);
    }

    public async Task<(Guid ClientId, Guid PersonId)> GetClientAsync(string email)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            select client.id, person.id
            from clients client
            join persons person on person.id = client.person_id
            where client.organization_id = @OrganizationId and person.email = @Email
            """,
            connection);
        command.Parameters.AddWithValue("OrganizationId", _organizationId);
        command.Parameters.AddWithValue("Email", email);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"Could not find the created student with email {email}.");
        return (reader.GetGuid(0), reader.GetGuid(1));
    }

    public async Task CreateClientLoginAsync(Guid personId, string email)
    {
        var hash = new Pbkdf2PasswordHasher().Hash(ClientPassword);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            insert into user_accounts (id, person_id, email, password_hash)
            values (@UserId, @PersonId, @Email, @PasswordHash);
            insert into person_roles (person_id, role_id)
            select @PersonId, id from roles where code = 'client'
            on conflict (person_id, role_id) do nothing;
            """,
            connection);
        command.Parameters.AddWithValue("UserId", Guid.NewGuid());
        command.Parameters.AddWithValue("PersonId", personId);
        command.Parameters.AddWithValue("Email", email);
        command.Parameters.AddWithValue("PasswordHash", hash);
        await command.ExecuteNonQueryAsync();
    }

    public async Task EnableClientCheckInAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "update organization_settings set allow_client_checkin = true where organization_id = @OrganizationId",
            connection);
        command.Parameters.AddWithValue("OrganizationId", _organizationId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<AttendanceRecord> CheckInAsync(string email, Guid lessonId)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri($"{ApiUrl}/api/v1/") };
        var loginResponse = await httpClient.PostAsJsonAsync("auth/login", new { email, password = ClientPassword });
        loginResponse.EnsureSuccessStatusCode();
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", login.RootElement.GetProperty("accessToken").GetString());
        httpClient.DefaultRequestHeaders.Add("X-Organization-Id", _organizationId.ToString());
        var response = await httpClient.PostAsJsonAsync("attendance/check-in", new { lessonId });
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Client check-in returned {(int)response.StatusCode}: {responseBody}{Environment.NewLine}{await GetCheckInDiagnosticsAsync(lessonId, email)}");
        }

        return (await response.Content.ReadFromJsonAsync<AttendanceRecord>())!;
    }

    private async Task<string> GetCheckInDiagnosticsAsync(Guid lessonId, string email)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            select lesson.starts_at, lesson.ends_at, now(), current_date,
                client.current_status::text,
                exists (
                    select 1 from person_modalities
                    where person_id = person.id and modality_id = lesson.modality_id
                )
            from lessons lesson
            join clients client on client.organization_id = lesson.organization_id
            join persons person on person.id = client.person_id
            where lesson.id = @LessonId and person.email = @Email
            """,
            connection);
        command.Parameters.AddWithValue("LessonId", lessonId);
        command.Parameters.AddWithValue("Email", email);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return "The lesson/client pair was not found in the test database.";
        }

        return $"Lesson {reader.GetDateTime(0):O}..{reader.GetDateTime(1):O}; database now {reader.GetDateTime(2):O}; "
            + $"database date {reader.GetDateTime(3):yyyy-MM-dd}; client status {reader.GetString(4)}; modality linked {reader.GetBoolean(5)}.";
    }

    public async Task<IReadOnlyList<Guid>> ListLessonsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select id from lessons where organization_id = @OrganizationId order by starts_at",
            connection);
        command.Parameters.AddWithValue("OrganizationId", _organizationId);
        var lessonIds = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lessonIds.Add(reader.GetGuid(0));
        }

        return lessonIds;
    }

    public async Task<IReadOnlyList<Guid>> WaitForGeneratedLessonsAsync(IPage page)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var lessonIds = await ListLessonsAsync();
            if (lessonIds.Count > 0)
            {
                return lessonIds;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"The schedule generation did not persist any lesson.{Environment.NewLine}Page: {await page.Locator("body").InnerTextAsync()}{Environment.NewLine}{ProcessOutput}");
    }

    public async Task<int> CountValidatedAttendanceAsync(Guid lessonId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select count(*) from attendances where lesson_id = @LessonId and status = 'present' and validated_at is not null",
            connection);
        command.Parameters.AddWithValue("LessonId", lessonId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<int> CountPaymentsAsync(Guid clientId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            select count(*)
            from payments payment
            join invoices invoice on invoice.id = payment.invoice_id
            where invoice.organization_id = @OrganizationId and invoice.client_id = @ClientId
            """,
            connection);
        command.Parameters.AddWithValue("OrganizationId", _organizationId);
        command.Parameters.AddWithValue("ClientId", clientId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await StopProcessAsync(_adminProcess);
        await StopProcessAsync(_apiProcess);
        await _database.DisposeAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        var migrationDirectory = Path.Combine(FindRepositoryRoot(), "database", "kiai-control-database", "migrations");
        foreach (var migrationPath in Directory.GetFiles(migrationDirectory, "*.sql").Order(StringComparer.Ordinal))
        {
            await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(migrationPath), connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task SetOrganizationTimezoneAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "update organization_settings set timezone = 'UTC' where organization_id = @OrganizationId",
            connection);
        command.Parameters.AddWithValue("OrganizationId", _organizationId);
        await command.ExecuteNonQueryAsync();
    }

    private static Process StartApplication(
        string assemblyPath,
        string workingDirectory,
        ConcurrentQueue<string> output,
        IReadOnlyDictionary<string, string> environment)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Expected application build at {assemblyPath}.", assemblyPath);
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var variable in environment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, eventArgs) => AddOutput(output, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AddOutput(output, eventArgs.Data);
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {assemblyPath}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task WaitForHttpAsync(string url, Process process, ConcurrentQueue<string> output)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Application exited with code {process.ExitCode}.{Environment.NewLine}{FormatOutput(output)}");
            }

            try
            {
                using var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Application did not become ready at {url}.{Environment.NewLine}{FormatOutput(output)}");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void AddOutput(ConcurrentQueue<string> output, string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            output.Enqueue(line);
            while (output.Count > 100)
            {
                output.TryDequeue(out _);
            }
        }
    }

    private static string FormatOutput(ConcurrentQueue<string> output) => string.Join(Environment.NewLine, output);

    private static async Task StopProcessAsync(Process? process)
    {
        if (process is null)
        {
            return;
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process.Dispose();
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "database", "kiai-control-database", "migrations", "001_initial_api_schema.sql")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository migrations directory.");
    }

    internal sealed class AttendanceRecord
    {
        public Guid Id { get; init; }
        public Guid LessonId { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
