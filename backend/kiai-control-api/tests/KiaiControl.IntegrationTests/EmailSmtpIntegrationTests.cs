using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KiaiControl.IntegrationTests;

public sealed class MailpitFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:v1.33.1")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithEnvironment("MP_SMTP_AUTH", "smtp-test:test-password")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8025))
        .Build();
    public string Host => _container.Hostname;
    public int SmtpPort => _container.GetMappedPublicPort(1025);
    public HttpClient Api { get; private set; } = default!;
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Api = new HttpClient { BaseAddress = new Uri($"http://{Host}:{_container.GetMappedPublicPort(8025)}"), Timeout = TimeSpan.FromSeconds(10) };
    }
    public async Task DisposeAsync() { Api.Dispose(); await _container.DisposeAsync(); }
}

public sealed class EmailSmtpIntegrationTests(MailpitFixture fixture) : IClassFixture<MailpitFixture>
{
    private ServiceProvider Provider(string security = "None", string password = "test-password", CaptureLogger? logger = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp", ["Email:FromAddress"] = "platform@example.test", ["Email:FromName"] = "Kiai Control",
            ["Email:ReplyToAddress"] = "support@example.test", ["Email:Smtp:Host"] = fixture.Host,
            ["Email:Smtp:Port"] = fixture.SmtpPort.ToString(), ["Email:Smtp:Security"] = security,
            ["Email:Smtp:Username"] = "smtp-test", ["Email:Smtp:Password"] = password, ["Email:Smtp:TimeoutSeconds"] = "3",
            ["PasswordRecovery:ResetUrl"] = "https://admin.example.test/reset-password"
        }).Build();
        var services = new ServiceCollection().AddLogging().AddKiaiEmail(configuration);
        if (logger is not null) services.AddSingleton<ILogger<SmtpEmailSender>>(logger);
        return services.BuildServiceProvider();
    }

    private async Task<JsonElement?> FindAsync(string subject)
    {
        var listing = await fixture.Api.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        foreach (var entry in listing.GetProperty("messages").EnumerateArray())
        {
            if (entry.GetProperty("Subject").GetString() != subject) continue;
            return await fixture.Api.GetFromJsonAsync<JsonElement>($"/api/v1/message/{entry.GetProperty("ID").GetString()}");
        }
        return null;
    }

    [Fact]
    public async Task Smtp_ShouldAuthenticateAndDeliverUnicodeTextAndHtmlWithConfiguredSender()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var subject = $"Mensagem João {Guid.NewGuid():N}";
        var result = await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(new()
        {
            To = new("person@example.test", "João"), Subject = subject,
            TextBody = "Olá João, mensagem em texto.", HtmlBody = "<p>Olá <strong>João</strong>.</p>"
        });
        Assert.Equal(EmailSendStatus.Accepted, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.MessageId));
        Assert.EndsWith("@example.test", result.MessageId);
        var delivered = (await FindAsync(subject))!.Value;
        Assert.Equal("platform@example.test", delivered.GetProperty("From").GetProperty("Address").GetString());
        Assert.Contains("Olá João", delivered.GetProperty("Text").GetString());
        Assert.Contains("<strong>João</strong>", delivered.GetProperty("HTML").GetString());
        Assert.Equal("person@example.test", delivered.GetProperty("To")[0].GetProperty("Address").GetString());
    }

    [Fact]
    public async Task MessageOverrides_ShouldUseRequestedFromAndReplyToWithoutChangingGlobalOptions()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var subject = $"Overrides {Guid.NewGuid():N}";
        var result = await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(new()
        {
            To = new("person@example.test"), From = new("academy@example.test", "Academia"),
            ReplyTo = new("helpdesk@example.test", "Suporte"), Subject = subject, TextBody = "Mensagem"
        });
        Assert.Equal(EmailSendStatus.Accepted, result.Status);
        var delivered = (await FindAsync(subject))!.Value;
        Assert.Equal("academy@example.test", delivered.GetProperty("From").GetProperty("Address").GetString());
        var raw = await fixture.Api.GetStringAsync($"/api/v1/message/{delivered.GetProperty("ID").GetString()}/raw");
        Assert.Contains("helpdesk@example.test", raw);
        Assert.DoesNotContain("support@example.test", raw);
    }

    [Fact]
    public async Task AuthenticationFailure_ShouldReturnStructuredFailureWithoutLoggingSecretsOrMessageContent()
    {
        var logger = new CaptureLogger();
        const string password = "secret-key-not-for-logs";
        using var provider = Provider(password: password, logger: logger);
        using var scope = provider.CreateScope();
        var subject = $"Private subject {Guid.NewGuid():N}";
        var result = await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(new()
        { To = new("private-person@example.test"), Subject = subject, TextBody = "private-token-in-body" });
        Assert.Equal(EmailSendStatus.Failed, result.Status);
        Assert.Equal(EmailFailureKind.Authentication, result.FailureKind);
        Assert.False(result.IsTransient);
        Assert.Null(await FindAsync(subject));
        var logs = string.Join("\n", logger.Entries);
        Assert.Contains("Authentication", logs);
        foreach (var secret in new[] { password, subject, "private-person@example.test", "private-token-in-body" }) Assert.DoesNotContain(secret, logs);
    }

    [Theory]
    [InlineData("StartTls")]
    [InlineData("SslOnConnect")]
    public async Task RequiredEncryption_ShouldNeverDowngradeToPlaintextWhenServerHasNoTls(string security)
    {
        using var provider = Provider(security);
        using var scope = provider.CreateScope();
        var subject = $"TLS required {Guid.NewGuid():N}";
        var result = await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(new()
        { To = new("person@example.test"), Subject = subject, TextBody = "Do not send unencrypted." });
        Assert.Equal(EmailSendStatus.Failed, result.Status);
        Assert.Null(await FindAsync(subject));
    }

    [Fact]
    public async Task Recovery_ShouldUseSharedSenderAndSendTokenOnlyInLinkFragment()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var token = Guid.NewGuid().ToString("N");
        await scope.ServiceProvider.GetRequiredService<IPasswordRecoveryDelivery>().SendAsync("recovery@example.test", token, default);
        var delivered = (await FindAsync("Recuperação de acesso · Kiai Control"))!.Value;
        var text = delivered.GetProperty("Text").GetString()!;
        Assert.Contains($"#token={token}", text);
        Assert.DoesNotContain($"?token={token}", text);
        Assert.Contains("30 minutos", text);
    }

    public sealed class CaptureLogger : ILogger<SmtpEmailSender>
    {
        public List<string> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add(formatter(state, exception));
    }
}
