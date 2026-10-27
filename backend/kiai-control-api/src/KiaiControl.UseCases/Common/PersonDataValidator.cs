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
            throw new ArgumentException("FullName is required.");
        }

        var value = fullName.Trim();
        if (value.Length > 180)
        {
            throw new ArgumentException("FullName must contain between 1 and 180 characters.");
        }

        return value;
    }

    public static string? NormalizeOptional(string? value, string field, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maxLength)
        {
            throw new ArgumentException($"{field} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    public static string? NormalizeEmail(string? email)
    {
        var normalized = NormalizeOptional(email, "Email", 160);
        if (normalized is not null && (!MailAddress.TryCreate(normalized, out var address) ||
                                       !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Email is not a valid email address.");
        }

        return normalized?.ToLowerInvariant();
    }

    public static string? NormalizeDocument(string? document)
    {
        var normalized = NormalizeOptional(document, "Document", 30);
        if (normalized is not null && normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not ('.' or '/' or '-' or ' ')))
        {
            throw new ArgumentException("Document contains unsupported characters.");
        }

        return normalized;
    }

    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }

        var normalized = status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException("Status must be one of: active, inactive, suspended, canceled, trial.");
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
            throw new ArgumentException("BranchId must identify a branch in this organization.");
        }
    }
}