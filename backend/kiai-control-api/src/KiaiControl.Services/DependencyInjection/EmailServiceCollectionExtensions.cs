using System.Net.Mail;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Services.Auth;
using KiaiControl.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace KiaiControl.Services.DependencyInjection;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailProvider<TSender>(this IServiceCollection services, string name)
        where TSender : class, IEmailSender
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var key = name.Trim();
        services.AddSingleton(new EmailProviderRegistration(key));
        services.AddKeyedScoped<IEmailSender, TSender>(key);
        return services;
    }

    public static IServiceCollection AddKiaiEmail(this IServiceCollection services, IConfiguration configuration, bool allowInsecureLocalhost = false)
    {
        services.AddEmailProvider<DisabledEmailSender>("Disabled");
        services.AddEmailProvider<SmtpEmailSender>("Smtp");
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddOptions<EmailOptions>().Configure(options =>
        {
            configuration.GetSection(EmailOptions.SectionName).Bind(options);
            // Keep existing deployments working while migrating Smtp:* to Email:*.
            if (configuration["Email:Provider"] is null && string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"])
                && !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
            {
                options.Provider = "Smtp";
                options.Smtp.Host = configuration["Smtp:Host"]!;
                options.Smtp.Port = configuration.GetValue("Smtp:Port", 587);
                options.Smtp.Security = options.Smtp.Port == 465 ? SmtpConnectionSecurity.SslOnConnect : SmtpConnectionSecurity.StartTls;
                options.Smtp.Username = configuration["Smtp:Username"];
                options.Smtp.Password = configuration["Smtp:Password"];
                if (MailAddress.TryCreate(configuration["Smtp:From"], out var from))
                {
                    options.FromAddress = from.Address;
                    if (!string.IsNullOrWhiteSpace(from.DisplayName)) options.FromName = from.DisplayName;
                }
            }
        }).ValidateOnStart();

        services.AddSingleton<IValidateOptions<PasswordRecoveryEmailOptions>>(new PasswordRecoveryEmailOptionsValidator(allowInsecureLocalhost));
        services.AddOptions<PasswordRecoveryEmailOptions>().Configure(options =>
        {
            options.ResetUrl = configuration["PasswordRecovery:ResetUrl"] ?? configuration["Smtp:ResetUrl"];
        }).ValidateOnStart();

        services.TryAddScoped<IEmailSender>(provider =>
        {
            var selected = provider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider.Trim();
            var registration = provider.GetServices<EmailProviderRegistration>()
                .Last(item => item.Name.Equals(selected, StringComparison.OrdinalIgnoreCase));
            return new SelectedEmailSender(provider.GetRequiredKeyedService<IEmailSender>(registration.Name));
        });
        services.AddScoped<IPasswordRecoveryDelivery, PasswordRecoveryEmailDelivery>();
        return services;
    }

    // The keyed registration owns disposal. The facade avoids disposing a custom SDK adapter twice.
    private sealed class SelectedEmailSender(IEmailSender inner) : IEmailSender
    {
        public bool IsEnabled => inner.IsEnabled;
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            inner.SendAsync(message, cancellationToken);
    }
}
