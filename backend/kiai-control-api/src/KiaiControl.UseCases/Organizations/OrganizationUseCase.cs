using System.Net.Mail;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Organizations;

public sealed class OrganizationUseCase(IOrganizationRepository organizationRepository, IPasswordHasher passwordHasher)
{
    public async Task<OrganizationRegistrationResponse?> RegisterAsync(
        RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateOrganization(request.Name, request.TradeName, request.Document, request.Email, request.Phone);
        ValidatePersonName(request.OwnerFullName);
        ValidateEmail(request.OwnerEmail, "OwnerEmail");

        if (request.OwnerPassword.Length < 12)
        {
            throw new ArgumentException(SystemMessages.OwnerPasswordMustHaveAtLeast12Characters);
        }

        var organization = await organizationRepository.RegisterAsync(
            new Organization
            {
                Name = request.Name.Trim(),
                TradeName = Normalize(request.TradeName),
                Document = Normalize(request.Document),
                Email = Normalize(request.Email)?.ToLowerInvariant(),
                Phone = Normalize(request.Phone)
            },
            request.OwnerFullName.Trim(),
            request.OwnerEmail.Trim().ToLowerInvariant(),
            passwordHasher.Hash(request.OwnerPassword),
            cancellationToken);

        return organization is null
            ? null
            : new OrganizationRegistrationResponse
            {
                Organization = ToResponse(organization.Value.Organization),
                OwnerUserId = organization.Value.OwnerUserId,
                OwnerEmail = request.OwnerEmail.Trim().ToLowerInvariant()
            };
    }

    public async Task<OrganizationResponse?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        return organization is null ? null : ToResponse(organization);
    }

    public async Task<OrganizationResponse?> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateOrganization(request.Name, request.TradeName, request.Document, request.Email, request.Phone);
        var organization = await organizationRepository.UpdateAsync(new Organization
        {
            Id = organizationId,
            Name = request.Name.Trim(),
            TradeName = Normalize(request.TradeName),
            Document = Normalize(request.Document),
            Email = Normalize(request.Email)?.ToLowerInvariant(),
            Phone = Normalize(request.Phone)
        }, cancellationToken);
        return organization is null ? null : ToResponse(organization);
    }

    private static void ValidateOrganization(string name, string? tradeName, string? document, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            throw new ArgumentException(SystemMessages.NameIsRequiredAndMustNotExceed160Characters);
        }

        if (tradeName?.Length > 160 || document?.Length > 30 || phone?.Length > 30)
        {
            throw new ArgumentException(SystemMessages.OneOrMoreOrganizationFieldsExceedTheirMaximumLength);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            ValidateEmail(email, "Email");
        }
    }

    private static void ValidatePersonName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 180)
        {
            throw new ArgumentException(SystemMessages.OwnerFullNameIsRequiredAndMustNotExceed180Characters);
        }
    }

    private static void ValidateEmail(string email, string fieldName)
    {
        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException(string.Format(SystemMessages.MustBeAValidEMailAddress, fieldName));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrganizationResponse ToResponse(Organization organization)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            TradeName = organization.TradeName,
            Document = organization.Document,
            Email = organization.Email,
            Phone = organization.Phone,
            Status = organization.Status,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}