using System.ComponentModel.DataAnnotations;
using KiaiControl.Contracts.Auth;

namespace KiaiControl.UnitTests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Aa!bc", false)]
    [InlineData("abcdef", false)]
    [InlineData("ABCDEF", false)]
    [InlineData("123456", false)]
    [InlineData("abc!12", false)]
    [InlineData("ABC!12", false)]
    [InlineData("Aa1234", false)]
    [InlineData("Aa    ", false)]
    [InlineData("Aa\t\n12", false)]
    [InlineData("Aa!bcd", true)]
    [InlineData("Aa!123", true)]
    [InlineData("Áé!123", true)]
    [InlineData("Aa£123", true)]
    [InlineData("Aa😀bc", false)]
    [InlineData("Aa😀bcd", true)]
    public void Passwords_ShouldRequireSixCharactersUppercaseLowercaseAndSpecialCharacter(string? password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.IsValid(password));
    }

    [Fact]
    public void FormValidation_ShouldUseTheSamePolicyAndMessage()
    {
        var model = new PasswordForm { Password = "Aa!bc" };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true));
        Assert.Equal(PasswordPolicy.Requirements, Assert.Single(errors).ErrorMessage);
        model.Password = "Aa!bcd";
        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), [], validateAllProperties: true));
    }

    private sealed class PasswordForm
    {
        [ValidPassword]
        public string Password { get; set; } = "";
    }
}
