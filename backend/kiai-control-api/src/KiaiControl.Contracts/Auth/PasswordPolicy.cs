using System.Text;

namespace KiaiControl.Contracts.Auth;

public static class PasswordPolicy
{
    public const int MinimumLength = 6;

    public const string Requirements = "A senha deve ter no mínimo 6 caracteres, incluindo ao menos uma letra maiúscula, uma letra minúscula e um caractere especial.";

    public static bool IsValid(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var characterCount = 0;
        var hasUppercase = false;
        var hasLowercase = false;
        var hasSpecialCharacter = false;
        foreach (var character in password.EnumerateRunes())
        {
            characterCount++;
            hasUppercase |= Rune.IsUpper(character);
            hasLowercase |= Rune.IsLower(character);
            hasSpecialCharacter |= Rune.IsPunctuation(character) || Rune.IsSymbol(character);
        }

        return characterCount >= MinimumLength && hasUppercase && hasLowercase && hasSpecialCharacter;
    }
}
