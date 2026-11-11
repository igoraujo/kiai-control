using System.ComponentModel.DataAnnotations;

namespace KiaiControl.Contracts.Auth;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ValidPasswordAttribute : ValidationAttribute
{
    public ValidPasswordAttribute() : base(PasswordPolicy.Requirements)
    {
    }

    public override bool IsValid(object? value) => PasswordPolicy.IsValid(value as string);
}
