import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import 'mobile_models.dart';
import 'models.dart';

class ApiException implements Exception {
  const ApiException(this.message, this.statusCode);
  final String message;
  final int statusCode;
  @override
  String toString() => message;
}

abstract class SessionStorage {
  Future<String?> read(String key);
  Future<void> write(String key, String value);
  Future<void> delete(String key);
}

class SecureSessionStorage implements SessionStorage {
  final _storage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock_this_device,
    ),
  );
  @override
  Future<String?> read(String key) => _storage.read(key: key);
  @override
  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);
  @override
  Future<void> delete(String key) => _storage.delete(key: key);
}

class ApiClient {
  ApiClient({http.Client? httpClient, SessionStorage? storage, String? baseUrl})
    : _http = httpClient ?? http.Client(),
      _storage = storage ?? SecureSessionStorage(),
      _baseUrl =
          (baseUrl ??
                  const String.fromEnvironment(
                    'API_BASE_URL',
                    defaultValue: 'http://10.0.2.2:5210/api/v1',
                  ))
              .replaceFirst(RegExp(r'/$'), '') {
    if (kReleaseMode && !Uri.parse(_baseUrl).isScheme('https')) {
      throw StateError('Configure API_BASE_URL com HTTPS para distribuição.');
    }
  }

  final http.Client _http;
  final SessionStorage _storage;
  final String _baseUrl;
  final _expired = StreamController<String>.broadcast();
  Stream<String> get sessionExpired => _expired.stream;
  Future<bool>? _refreshing;
  int _sessionGeneration = 0;
  Future<void> _storageWrites = Future.value();

  Future<T> _writeSession<T>(Future<T> Function() action) {
    final next = _storageWrites.then((_) => action());
    _storageWrites = next.then<void>(
      (_) {},
      onError: (Object error, StackTrace stack) {},
    );
    return next;
  }

  Future<bool> get hasSession async =>
      (await _storage.read('refresh_token'))?.isNotEmpty == true;

  Future<void> clearSession({
    bool notify = false,
    String message = 'Sua sessão expirou. Entre novamente.',
  }) async {
    _sessionGeneration++;
    await _writeSession(() async {
      await _storage.delete('access_token');
      await _storage.delete('refresh_token');
      await _storage.delete(
        'session_role',
      ); // Legacy key, never used for authorization.
    });
    if (notify && !_expired.isClosed) _expired.add(message);
  }

  Future<void> login(String email, String password) async {
    final generation = ++_sessionGeneration;
    final data = await requestObject(
      'POST',
      '/auth/login',
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    try {
      if (!await _saveTokens(data, generation)) {
        throw const ApiException(
          'Sessão encerrada durante o login. Entre novamente.',
          401,
        );
      }
    } on FormatException {
      throw const ApiException('Resposta de login inválida.', 502);
    }
  }

  Future<bool> _saveTokens(Map<String, dynamic> data, int generation) async {
    final access = requiredString(data, 'accessToken');
    final refresh = requiredString(data, 'refreshToken');
    return _writeSession(() async {
      if (generation != _sessionGeneration) return false;
      await _storage.write('access_token', access);
      await _storage.write('refresh_token', refresh);
      return true;
    });
  }

  Future<MobileContext> getContext() => _model(
    () async =>
        MobileContext.fromJson(await requestObject('GET', '/mobile/context')),
  );
  Future<MobileAccount> getAccount() => _model(
    () async =>
        MobileAccount.fromJson(await requestObject('GET', '/mobile/account')),
  );
  Future<MobileProgress> getProgress() => _model(
    () async =>
        MobileProgress.fromJson(await requestObject('GET', '/mobile/progress')),
  );
  Future<DashboardData> getDashboard() => _model(() async {
    final now = DateTime.now();
    final month = '${now.year}-${now.month.toString().padLeft(2, '0')}';
    return DashboardData.fromJson(
      await requestObject(
        'GET',
        '/reports/dashboard?from=$month-01&to=$month-${now.day.toString().padLeft(2, '0')}',
      ),
    );
  });
  Future<StudentJourney> getJourney(int page) => _model(
    () async => StudentJourney.fromJson(
      await requestObject('GET', '/portal/journey?page=$page&pageSize=25'),
    ),
  );
  Future<List<RankingEntry>> getRankings(bool year, int page) => _model(
    () async => (await requestList(
      'GET',
      '/mobile/rankings?yearOnly=$year&page=$page&pageSize=25',
    )).map(RankingEntry.fromJson).toList(),
  );
  Future<List<FitnessProgressRecord>> getGoalProgress(String goal, int page) =>
      _model(
        () async => (await requestList(
          'GET',
          '/portal/goals/$goal/progress?page=$page&pageSize=25',
        )).map(FitnessProgressRecord.fromJson).toList(),
      );
  Future<void> saveAccount(Map<String, dynamic> data) async {
    await _send('PUT', '/mobile/account', body: data);
  }

  Future<void> savePhoto(List<int> bytes, String contentType) async {
    await _send(
      'PUT',
      '/mobile/account/photo',
      body: {'base64': base64Encode(bytes), 'contentType': contentType},
    );
  }

  Future<void> removePhoto() async => _send('DELETE', '/mobile/account/photo');

  Future<List<PendingCheckIn>> getPending(int page) => _model(
    () async => (await requestList(
      'GET',
      '/attendance/pending?page=$page&pageSize=25',
    )).map(PendingCheckIn.fromJson).toList(),
  );
  Future<void> validateCheckIn(String id, String status) async {
    final saved = await requestObject(
      'PATCH',
      '/attendance/$id/validate',
      body: {'status': status},
    );
    if (saved['id'] != id ||
        saved['status'] != status ||
        saved['validatedAt'] == null) {
      throw const ApiException(
        'A API não confirmou a validação. Atualize a lista.',
        502,
      );
    }
  }

  Future<List<AppNotification>> getNotifications(int page) => _model(
    () async => (await requestList(
      'GET',
      '/notifications?page=$page&pageSize=25',
    )).map(AppNotification.fromJson).toList(),
  );
  Future<void> markNotificationRead(String id) async =>
      _send('PATCH', '/notifications/$id/read');

  Future<ClientProfile> getProfile() => _model(
    () async =>
        ClientProfile.fromJson(await requestObject('GET', '/mobile/profile')),
  );

  Future<List<ClientLesson>> getLessons() => _model(
    () async => (await requestList(
      'GET',
      '/mobile/lessons',
    )).map(ClientLesson.fromJson).toList(),
  );

  Future<TeacherProfile> getTeacherProfile() => _model(
    () async => TeacherProfile.fromJson(
      await requestObject('GET', '/mobile/teacher/profile'),
    ),
  );
  Future<List<TeacherLesson>> getTeacherLessons() => _model(() async {
    final now = DateTime.now().toUtc();
    final query = Uri(
      queryParameters: {
        'from': now.subtract(const Duration(days: 7)).toIso8601String(),
        'to': now.add(const Duration(days: 24)).toIso8601String(),
      },
    ).query;
    return (await requestList(
      'GET',
      '/mobile/teacher/lessons?$query',
    )).map(TeacherLesson.fromJson).toList();
  });
  Future<List<EligibleClient>> getEligibleClients(String lesson) => _model(
    () async => (await requestList(
      'GET',
      '/mobile/teacher/lessons/$lesson/eligible-clients',
    )).map(EligibleClient.fromJson).toList(),
  );
  Future<List<AttendanceRecord>> getLessonAttendances(String lesson) => _model(
    () async => (await requestList(
      'GET',
      '/mobile/teacher/lessons/$lesson/attendances',
    )).map(AttendanceRecord.fromJson).toList(),
  );
  Future<void> saveLessonAttendance(
    String lesson,
    Map<String, String> statuses,
  ) async {
    final saved = await requestList(
      'PUT',
      '/mobile/teacher/lessons/$lesson/attendances',
      body: {
        'entries': statuses.entries
            .map(
              (e) => {
                'clientId': e.key,
                'status': e.value,
                'attendanceDate': DateTime.now().toUtc().toIso8601String(),
              },
            )
            .toList(),
      },
    );
    if (saved.length != statuses.length ||
        saved.any(
          (r) =>
              statuses[r['clientId']] != r['status'] ||
              r['validatedAt'] == null,
        )) {
      throw const ApiException(
        'A API não confirmou a chamada. Atualize os dados.',
        502,
      );
    }
  }

  Future<FitnessOverview> getFitnessOverview() => _model(
    () async =>
        FitnessOverview.fromJson(await requestObject('GET', '/mobile/fitness')),
  );
  Future<void> checkIn(String lessonId) async {
    final saved = await requestObject(
      'POST',
      '/attendance/check-in',
      body: {'lessonId': lessonId},
    );
    if (saved['lessonId'] != lessonId ||
        saved['status'] != 'pending_confirmation' ||
        saved['validatedAt'] != null) {
      throw const ApiException(
        'A API não confirmou um check-in pendente. Atualize a agenda.',
        502,
      );
    }
  }

  Future<void> cancelCheckIn(String lessonId) async =>
      _send('DELETE', '/attendance/check-in/$lessonId');
  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    await _send(
      'POST',
      '/auth/change-password',
      body: {'currentPassword': currentPassword, 'newPassword': newPassword},
      allowRefresh: false,
      expireOnUnauthorized: false,
    );
  }

  Future<void> registerDevice(
    String installation,
    String token,
    String platform,
  ) async => _send(
    'PUT',
    '/mobile/devices',
    body: {
      'installationId': installation,
      'token': token,
      'platform': platform,
    },
  );
  Future<void> removeDevice(String installation) async =>
      _send('DELETE', '/mobile/devices/${Uri.encodeComponent(installation)}');
  Future<void> logout() async {
    final token = await _storage.read('refresh_token');
    try {
      if (token != null) {
        await _send(
          'POST',
          '/auth/logout',
          body: {'refreshToken': token},
          authenticated: false,
        );
      }
    } finally {
      await clearSession();
    }
  }

  Future<T> _model<T>(Future<T> Function() read) async {
    try {
      return await read();
    } on FormatException {
      throw const ApiException(
        'Resposta inválida da API. Tente novamente.',
        502,
      );
    } on TypeError {
      throw const ApiException(
        'Resposta inválida da API. Tente novamente.',
        502,
      );
    }
  }

  Future<Map<String, dynamic>> requestObject(
    String method,
    String path, {
    Map<String, dynamic>? body,
    bool authenticated = true,
  }) => _model(
    () async => object(
      jsonDecode(
        (await _send(
          method,
          path,
          body: body,
          authenticated: authenticated,
        )).body,
      ),
    ),
  );
  Future<List<Map<String, dynamic>>> requestList(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) => _model(
    () async =>
        objects(jsonDecode((await _send(method, path, body: body)).body)),
  );

  Future<http.Response> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
    bool authenticated = true,
    bool allowRefresh = true,
    bool expireOnUnauthorized = true,
  }) async {
    final token = authenticated ? await _storage.read('access_token') : null;
    final request = http.Request(method, Uri.parse('$_baseUrl$path'));
    request.headers['Accept'] = 'application/json';
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    if (body != null) {
      request.headers['Content-Type'] = 'application/json';
      request.body = jsonEncode(body);
    }
    final http.Response response;
    try {
      response = await (() async => http.Response.fromStream(
        await _http.send(request),
      ))().timeout(const Duration(seconds: 25));
    } on TimeoutException {
      throw const ApiException(
        'A API demorou para responder. Atualize os dados antes de repetir a ação.',
        0,
      );
    } on http.ClientException {
      throw const ApiException(
        'Não foi possível conectar à API. Verifique sua conexão.',
        0,
      );
    } on IOException {
      throw const ApiException(
        'Não foi possível conectar à API. Verifique sua conexão.',
        0,
      );
    }
    if (response.statusCode == 401 && authenticated && allowRefresh) {
      // Another request may already have rotated the token.
      final current = await _storage.read('access_token');
      if (current != token && current != null || await _refreshSession()) {
        return _send(method, path, body: body, allowRefresh: false);
      }
    }
    if (response.statusCode == 401 && authenticated && expireOnUnauthorized) {
      await clearSession(notify: true);
      throw const ApiException('Sua sessão expirou. Entre novamente.', 401);
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(
        _errorMessage(response, authenticated),
        response.statusCode,
      );
    }
    return response;
  }

  Future<bool> _refreshSession() async {
    if (_refreshing != null) return _refreshing!;
    final future = _refresh();
    _refreshing = future;
    try {
      return await future;
    } finally {
      if (identical(_refreshing, future)) _refreshing = null;
    }
  }

  Future<bool> _refresh() async {
    final generation = _sessionGeneration;
    final token = await _storage.read('refresh_token');
    if (token == null) return false;
    try {
      final data = await requestObject(
        'POST',
        '/auth/refresh',
        body: {'refreshToken': token},
        authenticated: false,
      );
      if (generation != _sessionGeneration) return false;
      return await _saveTokens(data, generation);
    } on ApiException catch (e) {
      if (e.statusCode == 401) return false;
      rethrow; // Network failures preserve the session for retry.
    } on FormatException {
      throw const ApiException('Resposta inválida ao renovar a sessão.', 502);
    }
  }

  String _errorMessage(http.Response response, bool authenticated) {
    try {
      final data = jsonDecode(response.body);
      if (data is String && data.isNotEmpty) return data;
      if (data is Map<String, dynamic>) {
        if (data['errors'] is Map) {
          return (data['errors'] as Map).values
              .expand((v) => v is List ? v : [v])
              .join('\n');
        }
        final message = data['detail'] ?? data['message'] ?? data['title'];
        if (message is String && message.isNotEmpty) return message;
      }
    } on FormatException {
      /* Do not display proxy HTML or internal errors. */
    }
    if (response.statusCode == 401) {
      return authenticated
          ? 'Senha atual incorreta ou sessão encerrada.'
          : 'E-mail ou senha inválidos.';
    }
    if (response.statusCode == 403) {
      return 'Você não tem permissão para esta ação.';
    }
    if (response.statusCode == 429) {
      return 'Muitas tentativas. Aguarde um minuto e tente novamente.';
    }
    return 'Não foi possível concluir a solicitação (${response.statusCode}).';
  }

  void dispose() {
    _http.close();
    _expired.close();
  }
}
