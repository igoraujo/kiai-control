using System.Net.Mail;
using Dapper;
using KiaiControl.Api.Configuration;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Interfaces;
using KiaiControl.Core.Entities;
using KiaiControl.Repositories.Queries.Platform;
using KiaiControl.Repositories.Queries.Audit;
using Microsoft.Extensions.Options;

namespace KiaiControl.Api.Services;

public sealed class PlatformOwnerBootstrapper(
    IDbConnectionFactory connectionFactory,
    IPasswordHasher passwordHasher,
    IOptions<PlatformOwnerOptions> options,
    ILogger<PlatformOwnerBootstrapper> logger)
{
    private static readonly Guid PlatformOrganizationId = Guid.Parse("da6ad84c-d663-47a8-b3ae-f3abcf7f1020");
    private static readonly Guid PlatformPersonId = Guid.Parse("b4a1acb6-f720-433f-8ff0-64887f25a409");
    private static readonly Guid PlatformUserId = Guid.Parse("019aff43-4885-71e5-9f38-9964974c4051");

    public async Task EnsureProvisionedAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        if (string.IsNullOrWhiteSpace(configured.Email)
            && string.IsNullOrWhiteSpace(configured.FullName)
            && string.IsNullOrWhiteSpace(configured.Password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configured.Email)
            || string.IsNullOrWhiteSpace(configured.FullName)
            || string.IsNullOrWhiteSpace(configured.Password))
        {
            throw new InvalidOperationException(KiaiControl.Api.Common.SystemMessages.PlatformOwnerConfigurationMustBeComplete);
        }

        if (!PasswordPolicy.IsValid(configured.Password))
        {
            throw new InvalidOperationException(PasswordPolicy.Requirements);
        }

        var email = new MailAddress(configured.Email.Trim()).Address.ToLowerInvariant();
        var fullName = configured.FullName.Trim();
        if (fullName.Length > 180)
        {
            throw new InvalidOperationException(KiaiControl.Api.Common.SystemMessages.InitialPlatformOwnerNameMustNotExceed180Characters);
        }

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var existingPlatformOwner = await connection.QuerySingleOrDefaultAsync<string>(
            PlatformBootstrapQueries.ExistingOwner,
            new { UserId = PlatformUserId },
            transaction);
        if (existingPlatformOwner is not null)
        {
            if (!existingPlatformOwner.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(KiaiControl.Api.Common.SystemMessages.PlatformOwnerWasAlreadyProvisionedWithAnotherEmail);
            }

            transaction.Commit();
            return;
        }

        var conflictingAccount = await connection.QuerySingleOrDefaultAsync<Guid?>(
            PlatformBootstrapQueries.ExistingEmail,
            new { Email = email },
            transaction);
        if (conflictingAccount is not null)
        {
            throw new InvalidOperationException(KiaiControl.Api.Common.SystemMessages.PlatformOwnerEmailAlreadyBelongsToAnotherAccount);
        }

        var now = DateTimeOffset.UtcNow;
        var passwordHash = passwordHasher.Hash(configured.Password);
        await connection.ExecuteAsync(PlatformBootstrapQueries.Create, new
        {
            OrganizationId = PlatformOrganizationId,
            PersonId = PlatformPersonId,
            UserId = PlatformUserId,
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHash,
            CreatedAt = now
        }, transaction);

        await connection.ExecuteAsync(AuditQueries.Insert, new AuditLog
        {
            Id = Guid.NewGuid(), OrganizationId = PlatformOrganizationId,
            ActorPersonId = PlatformPersonId, ActorUserAccountId = PlatformUserId,
            Action = "platform.account.provisioned", EntityName = "platform.accounts", EntityId = PlatformUserId,
            Metadata = "{\"source\":\"bootstrap\",\"mustChangePassword\":true}", CreatedAt = now
        }, transaction);

        transaction.Commit();
        logger.LogInformation("The configured platform owner account was provisioned.");
    }
}
