using System.Net.Mail;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Common;

public static class PersonDataValidator
{
    private static readonly string[] AllowedStatuses = ["active", "inactive", "suspended", "canceled", "trial"];

    public static string NormalizeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(SystemMessages.FullNameIsRequired);
        }

        var value = fullName.Trim();
        if (value.Length > 180)
        {
            throw new ArgumentException(SystemMessages.FullNameMustContainBetween1And180Characters);
        }

        return value;
    }

    public static string? NormalizeOptional(string? value, string field, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maxLength)
        {
            throw new ArgumentException(string.Format(SystemMessages.CannotExceedCharacters, field, maxLength));
        }

        return normalized;
    }

    public static string? NormalizeEmail(string? email)
    {
        var normalized = NormalizeOptional(email, "Email", 160);
        if (normalized is not null && (!MailAddress.TryCreate(normalized, out var address) ||
                                       !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(SystemMessages.EmailIsNotAValidEmailAddress);
        }

        return normalized?.ToLowerInvariant();
    }

    public static string? NormalizeDocument(string? document)
    {
        var normalized = NormalizeOptional(document, "Document", 30);
        if (normalized is not null && normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not ('.' or '/' or '-' or ' ')))
        {
            throw new ArgumentException(SystemMessages.DocumentContainsUnsupportedCharacters);
        }

        return normalized;
    }

    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(SystemMessages.StatusIsRequired);
        }

        var normalized = status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException(SystemMessages.StatusMustBeOneOfActiveInactiveSuspendedCanceledTrial);
        }

        return normalized;
    }

    public static async Task ValidateBranchAsync(
        IBranchRepository branchRepository,
        Guid organizationId,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (branchId is Guid id && await branchRepository.GetByIdAsync(organizationId, id, cancellationToken) is null)
        {
            throw new ArgumentException(SystemMessages.BranchIdMustIdentifyABranchInThisOrganization);
        }
    }
}