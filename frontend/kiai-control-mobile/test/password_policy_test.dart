import 'package:flutter_test/flutter_test.dart';
import 'package:kiai_control_mobile/src/password_policy.dart';

void main() {
  final cases = <String?, bool>{
    null: false,
    '': false,
    'Aa!bc': false,
    'abcdef': false,
    'ABCDEF': false,
    '123456': false,
    'abc!12': false,
    'ABC!12': false,
    'Aa1234': false,
    'Aa    ': false,
    'Aa\t\n12': false,
    'Aa!bcd': true,
    'Aa!123': true,
    'Áé!123': true,
    'Aa£123': true,
    'Aa😀bc': false,
    'Aa😀bcd': true,
  };

  for (final entry in cases.entries) {
    test('Password policy accepts ${entry.key} = ${entry.value}', () {
      expect(PasswordPolicy.isValid(entry.key), entry.value);
    });
  }
}
