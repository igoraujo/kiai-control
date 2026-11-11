using System.Net;
using System.Net.Sockets;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Services.DependencyInjection;
using KiaiControl.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KiaiControl.UnitTests;

public sealed class EmailServiceTests
{
    private static ServiceProvider Provider(Dictionary<string, string?>? values = null, bool development = false,
        Action<IServiceCollection>? register = null)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddKiaiEmail(new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build(), development);
        register?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static Dictionary<string, string?> Smtp() => new()
    {
        ["Email:Provider"] = "Smtp", ["Email:FromAddress"] = "no-reply@example.test",
        ["Email:Smtp:Host"] = "localhost", ["Email:Smtp:Port"] = "1025", ["Email:Smtp:Security"] = "None"
    };

    [Fact]
    public async Task DefaultConfiguration_ShouldDisableDeliveryWithoutReportingSuccess()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        Assert.False(sender.IsEnabled);
        Assert.Equal(EmailSendStatus.Disabled, (await sender.SendAsync(Message())).Status);
        Assert.False(scope.ServiceProvider.GetRequiredService<IPasswordRecoveryDelivery>().IsConfigured);
    }

    [Theory]
    [InlineData("Email:Provider", "NotRegistered")]
    [InlineData("Email:FromAddress", "invalid-address")]
    [InlineData("Email:FromName", "Name\r\nBcc:other@example.test")]
    [InlineData("Email:ReplyToAddress", "invalid-address")]
    [InlineData("Email:Smtp:Host", "smtp://localhost")]
    [InlineData("Email:Smtp:Port", "0")]
    [InlineData("Email:Smtp:Port", "65536")]
    [InlineData("Email:Smtp:TimeoutSeconds", "0")]
    [InlineData("Email:Smtp:TimeoutSeconds", "301")]
    [InlineData("Email:Smtp:Security", "999")]
    [InlineData("Email:Smtp:Username", "smtp-user")]
    [InlineData("Email:Smtp:Password", "secret-key-not-for-logs")]
    public void InvalidConfiguration_ShouldFailBeforeConnectingAndNeverIncludeSecrets(string key, string value)
    {
        var configuration = Smtp();
        configuration[key] = value;
        using var provider = Provider(configuration);
        using var scope = provider.CreateScope();
        var exception = Assert.Throws<OptionsValidationException>(() => scope.ServiceProvider.GetRequiredService<IEmailSender>());
        Assert.DoesNotContain("secret-key-not-for-logs", exception.Message);
    }

    [Theory]
    [InlineData("https://admin.example.test/reset-password", false, true)]
    [InlineData("http://localhost:5081/reset-password", true, true)]
    [InlineData("http://127.0.0.1:5081/reset-password", true, true)]
    [InlineData("http://localhost:5081/reset-password", false, false)]
    [InlineData("http://admin.example.test/reset-password", true, false)]
    [InlineData("https://user:password@admin.example.test/reset-password", false, false)]
    public void RecoveryUrl_ShouldRequireHttpsExceptDevelopmentLoopback(string url, bool development, bool valid)
    {
        using var provider = Provider(new() { ["PasswordRecovery:ResetUrl"] = url }, development);
        if (valid) Assert.Equal(url, provider.GetRequiredService<IOptions<PasswordRecoveryEmailOptions>>().Value.ResetUrl);
        else Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<PasswordRecoveryEmailOptions>>().Value);
    }

    [Fact]
    public async Task CustomProvider_ShouldBeSelectedByConfigurationAndReceiveRecoveryWithoutSmtpDependency()
    {
        using var provider = Provider(new()
        {
            ["Email:Provider"] = "customapi", ["Email:FromAddress"] = "no-reply@example.test",
            ["PasswordRecovery:ResetUrl"] = "https://admin.example.test/reset-password?lang=pt#old"
        }, register: services => services.AddEmailProvider<CapturingEmailSender>("CustomApi"));
        using var scope = provider.CreateScope();
        var sender = Assert.IsType<CapturingEmailSender>(scope.ServiceProvider.GetRequiredKeyedService<IEmailSender>("CustomApi"));
        var delivery = scope.ServiceProvider.GetRequiredService<IPasswordRecoveryDelivery>();
        Assert.True(delivery.IsConfigured);
        const string token = "private-token+/=";
        await delivery.SendAsync("person@example.test", token, default);
        var message = Assert.Single(sender.Messages);
        Assert.Equal("person@example.test", message.To.Address);
        var link = message.TextBody!.Split('\n').Single(line => line.StartsWith("https://", StringComparison.Ordinal));
        var url = new Uri(link);
        Assert.Equal("?lang=pt", url.Query);
        Assert.Equal(token, Uri.UnescapeDataString(url.Fragment["#token=".Length..]));
        Assert.DoesNotContain(token, url.Query);
    }

    [Fact]
    public async Task SelectedProvider_ShouldBeDisposedExactlyOnceWhenItsScopeEnds()
    {
        using var provider = Provider(new() { ["Email:Provider"] = "Disposable", ["Email:FromAddress"] = "no-reply@example.test" },
            register: services => services.AddEmailProvider<DisposableEmailSender>("Disposable"));
        DisposableEmailSender adapter;
        using (var scope = provider.CreateScope())
        {
            var selected = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            adapter = Assert.IsType<DisposableEmailSender>(scope.ServiceProvider.GetRequiredKeyedService<IEmailSender>("Disposable"));
            Assert.Equal(EmailSendStatus.Accepted, (await selected.SendAsync(Message())).Status);
        }
        Assert.Equal(1, adapter.DisposeCount);
    }

    [Fact]
    public void LegacySmtpConfiguration_ShouldMapToNewOptionsAndExplicitDisabledShouldTakePrecedence()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.example.test", ["Smtp:Port"] = "465", ["Smtp:From"] = "Support <support@example.test>",
            ["Smtp:Username"] = "smtp-user", ["Smtp:Password"] = "smtp-key", ["Smtp:ResetUrl"] = "https://admin.example.test/reset-password"
        };
        using (var provider = Provider(configuration))
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            Assert.Equal("Smtp", options.Provider);
            Assert.Equal("support@example.test", options.FromAddress);
            Assert.Equal("Support", options.FromName);
            Assert.Equal(SmtpConnectionSecurity.SslOnConnect, options.Smtp.Security);
            Assert.Equal(configuration["Smtp:ResetUrl"], provider.GetRequiredService<IOptions<PasswordRecoveryEmailOptions>>().Value.ResetUrl);
        }
        configuration["Email:Provider"] = "Disabled";
        using var disabled = Provider(configuration);
        using var scope = disabled.CreateScope();
        Assert.False(scope.ServiceProvider.GetRequiredService<IEmailSender>().IsEnabled);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("to")]
    [InlineData("from")]
    [InlineData("reply")]
    [InlineData("body")]
    public async Task InvalidMessage_ShouldFailBeforeNetworkAccess(string field)
    {
        using var provider = Provider(Smtp());
        using var scope = provider.CreateScope();
        var message = new EmailMessage
        {
            To = new(field == "to" ? "person@example.test\r\nBcc:other@example.test" : "person@example.test"),
            From = field == "from" ? new("not-an-address") : null,
            ReplyTo = field == "reply" ? new("bad-address") : null,
            Subject = field == "subject" ? "Hello\r\nBcc:other@example.test" : "Hello",
            TextBody = field == "body" ? null : "Message"
        };
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(message));
    }

    [Fact]
    public async Task Cancellation_ShouldPropagateInsteadOfReportingDeliveryFailure()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var configuration = Smtp();
            configuration["Email:Smtp:Port"] = ((IPEndPoint)listener.LocalEndpoint).Port.ToString();
            using var provider = Provider(configuration);
            using var scope = provider.CreateScope();
            using var cancellation = new CancellationTokenSource();
            var sending = scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(Message(), cancellation.Token);
            using var connection = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally { listener.Stop(); }
    }

    private static EmailMessage Message() => new() { To = new("person@example.test"), Subject = "Test", TextBody = "Message" };

    public sealed class CapturingEmailSender : IEmailSender
    {
        public bool IsEnabled => true;
        public List<EmailMessage> Messages { get; } = [];
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(new EmailSendResult(EmailSendStatus.Accepted, "custom-message-id"));
        }
    }

    public sealed class DisposableEmailSender : IEmailSender, IDisposable
    {
        public bool IsEnabled => true;
        public int DisposeCount { get; private set; }
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmailSendResult(EmailSendStatus.Accepted));
        public void Dispose() => DisposeCount++;
    }
}
