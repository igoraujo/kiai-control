import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:kiai_control_mobile/src/api_client.dart';
import 'package:kiai_control_mobile/src/app.dart';

import 'api_client_test.dart' show MemoryStorage;

void main() {
  ApiClient api({
    List<String> capabilities = const [],
    List<String> roles = const ['owner'],
    Future<http.Response> Function(http.Request)? handler,
  }) {
    final storage = MemoryStorage();
    storage.values.addAll({
      'access_token': 'access',
      'refresh_token': 'refresh',
    });
    return ApiClient(
      storage: storage,
      httpClient: MockClient((request) async {
        if (request.url.path.endsWith('/mobile/context')) {
          return http.Response(
            jsonEncode({
              'organizationName': 'Academia teste',
              'timezone': 'America/Sao_Paulo',
              'capabilities': capabilities,
              'roles': roles,
              'permissions': [],
              'mustChangePassword': false,
            }),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }
        return handler == null ? http.Response('[]', 200) : handler(request);
      }),
    );
  }

  testWidgets('an owner without capabilities sees no unauthorized actions', (
    tester,
  ) async {
    final client = api();
    addTearDown(client.dispose);
    await tester.pumpWidget(KiaiControlApp(api: client));
    await tester.pumpAndSettle();
    expect(find.text('Academia teste'), findsOneWidget);
    expect(find.textContaining('Seu acesso está ativo'), findsOneWidget);
    expect(find.text('Check-ins pendentes'), findsNothing);
    expect(find.text('Academia neste mês'), findsNothing);
  });

  testWidgets(
    'unknown roles retain access and navigation follows API capabilities',
    (tester) async {
      final client = api(
        roles: ['new_role'],
        capabilities: ['attendance.validate'],
      );
      addTearDown(client.dispose);
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(KiaiControlApp(api: client));
      await tester.pumpAndSettle();
      expect(find.text('Check-ins pendentes'), findsOneWidget);
      await tester.tap(find.text('Validar check-ins'));
      await tester.pumpAndSettle();
      expect(
        find.text('Não há check-ins pendentes nesta página.'),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'pending approval uses server response and removes validated record',
    (tester) async {
      var pending = true;
      final client = api(
        capabilities: ['attendance.validate'],
        handler: (request) async {
          if (request.method == 'PATCH') {
            expect(jsonDecode(request.body)['status'], 'present');
            pending = false;
            return http.Response(
              '{"id":"attendance","status":"present","validatedAt":"2026-11-16T10:00:00Z"}',
              200,
            );
          }
          return http.Response(
            jsonEncode(
              pending
                  ? [
                      {
                        'id': 'attendance',
                        'clientName': 'Aluno da academia',
                        'lessonId': 'lesson',
                        'status': 'pending_confirmation',
                        'modalityName': 'Jiu-jitsu',
                        'lessonStartsAt': '2026-11-16T10:00:00Z',
                        'attendanceDate': '2026-11-16T09:00:00Z',
                      },
                    ]
                  : [],
            ),
            200,
          );
        },
      );
      addTearDown(client.dispose);
      await tester.pumpWidget(KiaiControlApp(api: client));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Validar check-ins'));
      await tester.pumpAndSettle();
      expect(find.text('Pendente de validação'), findsOneWidget);
      await tester.tap(find.text('Validar'));
      await tester.pumpAndSettle();
      expect(pending, isFalse);
      expect(
        find.text('Não há check-ins pendentes nesta página.'),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'invalid dashboard data shows retry without rendering fake values',
    (tester) async {
      final client = api(
        capabilities: ['reports.dashboard'],
        handler: (r) async => http.Response('{}', 200),
      );
      addTearDown(client.dispose);
      await tester.pumpWidget(KiaiControlApp(api: client));
      await tester.pumpAndSettle();
      expect(
        find.text('Resposta inválida da API. Tente novamente.'),
        findsOneWidget,
      );
      expect(find.text('Tentar novamente'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('expired session returns to login', (tester) async {
    final client = api();
    addTearDown(client.dispose);
    await tester.pumpWidget(KiaiControlApp(api: client));
    await tester.pumpAndSettle();
    await client.clearSession(notify: true);
    await tester.pumpAndSettle();
    expect(find.text('Entrar'), findsOneWidget);
    expect(find.text('Sua sessão expirou. Entre novamente.'), findsOneWidget);
  });
}
