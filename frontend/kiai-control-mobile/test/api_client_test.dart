import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:kiai_control_mobile/src/api_client.dart';

class MemoryStorage implements SessionStorage {
  final values = <String, String>{};
  @override
  Future<String?> read(String key) async => values[key];
  @override
  Future<void> write(String key, String value) async {
    values[key] = value;
  }

  @override
  Future<void> delete(String key) async {
    values.remove(key);
  }
}

void main() {
  late MemoryStorage storage;
  setUp(() {
    storage = MemoryStorage();
    storage.values.addAll({'access_token': 'old', 'refresh_token': 'refresh'});
  });
  ApiClient client(Future<http.Response> Function(http.Request) handler) =>
      ApiClient(
        storage: storage,
        baseUrl: 'https://api.example.test/api/v1',
        httpClient: MockClient(handler),
      );

  test('accepts all API roles and persists both session tokens', () async {
    final api = client(
      (r) async => http.Response(
        jsonEncode({
          'accessToken': 'access-owner',
          'refreshToken': 'refresh-owner',
          'user': {
            'roles': ['owner', 'custom_role'],
            'permissions': ['reports.read'],
          },
        }),
        200,
      ),
    );
    addTearDown(api.dispose);
    await api.login('owner@example.test', 'Password!');
    expect(storage.values['access_token'], 'access-owner');
    expect(storage.values['refresh_token'], 'refresh-owner');
  });

  test(
    'incomplete login and dashboard payloads are visible API errors',
    () async {
      final api = client((r) async => http.Response('{}', 200));
      addTearDown(api.dispose);
      await expectLater(
        api.login('user@example.test', 'Password!'),
        throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 502)),
      );
      await expectLater(
        api.getDashboard(),
        throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 502)),
      );
    },
  );

  test('concurrent 401s rotate the refresh token only once', () async {
    var refreshes = 0;
    final api = client((r) async {
      if (r.url.path.endsWith('/auth/refresh')) {
        refreshes++;
        await Future<void>.delayed(const Duration(milliseconds: 15));
        expect(jsonDecode(r.body)['refreshToken'], 'refresh');
        return http.Response(
          '{"accessToken":"next","refreshToken":"rotated"}',
          200,
        );
      }
      if (r.headers['Authorization'] == 'Bearer old') {
        return http.Response('', 401);
      }
      expect(r.headers['Authorization'], 'Bearer next');
      return http.Response('{"ok":true}', 200);
    });
    addTearDown(api.dispose);
    final responses = await Future.wait(
      List.generate(4, (_) => api.requestObject('GET', '/resource')),
    );
    expect(refreshes, 1);
    expect(responses.every((r) => r['ok'] == true), isTrue);
    expect(storage.values['refresh_token'], 'rotated');
  });

  test('failed refresh expires the session and notifies the shell', () async {
    final api = client((r) async => http.Response('', 401));
    addTearDown(api.dispose);
    final expired = api.sessionExpired.first;
    await expectLater(
      api.requestObject('GET', '/resource'),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 401)),
    );
    await expired;
    expect(await api.hasSession, isFalse);
  });

  test('network failure during refresh preserves session for retry', () async {
    final api = client(
      (r) async =>
          http.Response('', r.url.path.endsWith('/auth/refresh') ? 503 : 401),
    );
    addTearDown(api.dispose);
    await expectLater(
      api.requestObject('GET', '/resource'),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 503)),
    );
    expect(await api.hasSession, isTrue);
  });

  test(
    'malformed and falsely approved check-in responses never report success',
    () async {
      for (final body in [
        '',
        '{}',
        '{"lessonId":"lesson","status":"present"}',
        '{"lessonId":"other","status":"pending_confirmation"}',
      ]) {
        final api = client((r) async => http.Response(body, 201));
        await expectLater(
          api.checkIn('lesson'),
          throwsA(
            isA<ApiException>().having((e) => e.statusCode, 'status', 502),
          ),
        );
        api.dispose();
      }
    },
  );

  test('accepts pending check-in only when API confirms its lesson', () async {
    final api = client(
      (r) async => http.Response(
        '{"lessonId":"lesson","status":"pending_confirmation","validatedAt":null}',
        201,
      ),
    );
    addTearDown(api.dispose);
    await api.checkIn('lesson');
  });

  test('API validation errors and forbidden actions remain visible', () async {
    final api = client(
      (r) async =>
          http.Response('{"errors":{"FullName":["Nome obrigatório."]}}', 400),
    );
    addTearDown(api.dispose);
    await expectLater(
      api.saveAccount({}),
      throwsA(
        isA<ApiException>().having(
          (e) => e.message,
          'message',
          'Nome obrigatório.',
        ),
      ),
    );
  });

  test('wrong current password does not erase a valid session', () async {
    final api = client((r) async => http.Response('', 401));
    addTearDown(api.dispose);
    await expectLater(
      api.changePassword(currentPassword: 'wrong', newPassword: 'Newpass!'),
      throwsA(isA<ApiException>()),
    );
    expect(await api.hasSession, isTrue);
  });

  test(
    'logout clears local tokens even when server cannot be reached',
    () async {
      final api = client((r) async => throw http.ClientException('offline'));
      addTearDown(api.dispose);
      await expectLater(api.logout(), throwsA(isA<ApiException>()));
      expect(await api.hasSession, isFalse);
    },
  );
}
