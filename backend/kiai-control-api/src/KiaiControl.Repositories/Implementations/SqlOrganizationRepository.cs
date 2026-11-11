using Dapper;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Queries.Organizations;
using KiaiControl.Repositories.Queries.Audit;
using Npgsql;

namespace KiaiControl.Repositories.Implementations;

public sealed class SqlOrganizationRepository(IDbConnectionFactory dbConnectionFactory) : IOrganizationRepository
{
    public async Task<(Organization Organization, Guid OwnerUserId)?> RegisterAsync(
        Organization organization,
        string ownerFullName,
        string ownerEmail,
        string ownerPasswordHash,
        CancellationToken cancellationToken = default,
        AuditLog? audit = null)
    {
        organization.Id = Guid.NewGuid();
        organization.CreatedAt = DateTimeOffset.UtcNow;
        var ownerUserId = Guid.NewGuid();
        var parameters = new
        {
            organization.Id,
            organization.Name,
            organization.TradeName,
            organization.Document,
            organization.Email,
            organization.Phone,
            organization.CreatedAt,
            PersonId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            OwnerFullName = ownerFullName,
            OwnerEmail = ownerEmail,
            OwnerPasswordHash = ownerPasswordHash
        };

        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await connection.QuerySingleOrDefaultAsync<OrganizationRegistrationRow>(
                new CommandDefinition(OrganizationQueries.Register, parameters, transaction, cancellationToken: cancellationToken));
            if (result is null)
                return null;
            if (audit is not null)
            {
                audit.Id = Guid.NewGuid();
                audit.CreatedAt = DateTimeOffset.UtcNow;
                audit.OrganizationId = organization.Id;
                audit.EntityId = organization.Id;
                await connection.ExecuteAsync(new CommandDefinition(AuditQueries.Insert, audit, transaction, cancellationToken: cancellationToken));
            }
            transaction.Commit();
            return result is null ? null : (result.ToOrganization(), result.OwnerUserId);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }
    }

    public async Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Organization>(OrganizationQueries.GetById, new { OrganizationId = organizationId });
    }

    public async Task<IReadOnlyList<Organization>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        var organizations = await connection.QueryAsync<Organization>(OrganizationQueries.List);
        return organizations.ToList();
    }

    public async Task<Organization?> UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        organization.UpdatedAt = DateTimeOffset.UtcNow;
        using var connection = await dbConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Organization>(OrganizationQueries.Update, organization);
    }

    private sealed class OrganizationRegistrationRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? TradeName { get; init; }

        public string? Document { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string Status { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public Guid OwnerUserId { get; init; }

        public Organization ToOrganization()
        {
            return new Organization
            {
                Id = Id,
                Name = Name,
                TradeName = TradeName,
                Document = Document,
                Email = Email,
                Phone = Phone,
                Status = Status,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
