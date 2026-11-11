abstract final class PasswordPolicy {
  static const minimumLength = 6;
  static const requirements =
      'A senha deve ter no mínimo 6 caracteres, incluindo ao menos uma letra maiúscula, uma letra minúscula e um caractere especial.';

  static final _uppercase = RegExp(r'\p{Lu}', unicode: true);
  static final _lowercase = RegExp(r'\p{Ll}', unicode: true);
  static final _special = RegExp(r'[\p{P}\p{S}]', unicode: true);

  static bool isValid(String? password) =>
      password != null &&
      password.runes.length >= minimumLength &&
      _uppercase.hasMatch(password) &&
      _lowercase.hasMatch(password) &&
      _special.hasMatch(password);
}
