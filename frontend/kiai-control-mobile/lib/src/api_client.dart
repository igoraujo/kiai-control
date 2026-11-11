import 'dart:convert';
import 'dart:io';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import 'models.dart';

class ApiException implements Exception {
  const ApiException(this.message, this.statusCode);

  final String message;
  final int statusCode;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient({
    http.Client? httpClient,
    FlutterSecureStorage? secureStorage,
  })  : _http = httpClient ?? http.Client(),
        _storage = secureStorage ?? const FlutterSecureStorage();

  static const _apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5210/api/v1',
  );
  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _sessionRoleKey = 'session_role';

  final http.Client _http;
  final FlutterSecureStorage _storage;

  Future<bool> get hasSession async =>
      (await _storage.read(key: _refreshTokenKey))?.isNotEmpty == true;

  Future<String?> get sessionRole => _storage.read(key: _sessionRoleKey);

  Future<void> saveSessionRole(String role) =>
      _storage.write(key: _sessionRoleKey, value: role);

  Future<void> clearSession() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
    await _storage.delete(key: _sessionRoleKey);
  }

  Future<String> login(String email, String password) async {
    final response = await _send(
      'POST',
      '/auth/login',
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    final payload = _decodeObject(response.body);
    final accessToken = payload['accessToken'] as String?;
    final refreshToken = payload['refreshToken'] as String?;
    final user = payload['user'];
    final roles = user is Map<String, dynamic>
        ? (user['roles'] as List<dynamic>? ?? const [])
            .whereType<String>()
            .map((role) => role.toLowerCase())
            .toSet()
        : <String>{};
    final role = roles.contains('teacher')
        ? 'teacher'
        : roles.contains('client')
            ? 'client'
            : null;
    if (accessToken == null || refreshToken == null) {
      throw const ApiException('Resposta de login invalida.', 500);
    }
    if (role == null) {
      throw const ApiException(
        'Este aplicativo está disponível para alunos e professores.',
        403,
      );
    }

    await _storage.write(key: _accessTokenKey, value: accessToken);
    await _storage.write(key: _refreshTokenKey, value: refreshToken);
    await _storage.write(key: _sessionRoleKey, value: role);
    return role;
  }

  Future<ClientProfile> getProfile() async => ClientProfile.fromJson(
        _decodeObject((await _send('GET', '/mobile/profile')).body),
      );

  Future<List<ClientLesson>> getLessons() async {
    final start = DateTime.now().toUtc();
    final end = start.add(const Duration(days: 30));
    final query = Uri(queryParameters: {
      'from': start.toIso8601String(),
      'to': end.toIso8601String(),
    }).query;
    final response = await _send('GET', '/mobile/lessons?$query');
    return _decodeList(response.body)
        .map((item) => ClientLesson.fromJson(item))
        .toList();
  }

  Future<TeacherProfile> getTeacherProfile() async => TeacherProfile.fromJson(
        _decodeObject((await _send('GET', '/mobile/teacher/profile')).body),
      );

  Future<List<TeacherLesson>> getTeacherLessons() async {
    final now = DateTime.now().toUtc();
    final start = now.subtract(const Duration(days: 7));
    final end = now.add(const Duration(days: 24));
    final query = Uri(queryParameters: {
      'from': start.toIso8601String(),
      'to': end.toIso8601String(),
    }).query;
    final response = await _send('GET', '/mobile/teacher/lessons?$query');
    return _decodeList(response.body)
        .map((item) => TeacherLesson.fromJson(item))
        .toList();
  }

  Future<List<EligibleClient>> getEligibleClients(String lessonId) async {
    final response = await _send(
      'GET',
      '/mobile/teacher/lessons/$lessonId/eligible-clients',
    );
    return _decodeList(response.body)
        .map((item) => EligibleClient.fromJson(item))
        .toList();
  }

  Future<List<AttendanceRecord>> getLessonAttendances(String lessonId) async {
    final response = await _send(
      'GET',
      '/mobile/teacher/lessons/$lessonId/attendances',
    );
    return _decodeList(response.body)
        .map((item) => AttendanceRecord.fromJson(item))
        .toList();
  }

  Future<void> saveLessonAttendance(
    String lessonId,
    Map<String, String> statuses,
  ) async {
    await _send(
      'PUT',
      '/mobile/teacher/lessons/$lessonId/attendances',
      body: {
        'entries': statuses.entries
            .map((entry) => {
                  'clientId': entry.key,
                  'attendanceDate': DateTime.now().toUtc().toIso8601String(),
                  'status': entry.value,
                })
            .toList(),
      },
    );
  }

  Future<FitnessOverview> getFitnessOverview() async =>
      FitnessOverview.fromJson(
        _decodeObject((await _send('GET', '/mobile/fitness')).body),
      );

  Future<void> checkIn(String lessonId) async {
    await _send(
      'POST',
      '/attendance/check-in',
      body: {'lessonId': lessonId},
    );
  }

  Future<void> cancelCheckIn(String lessonId) async {
    await _send('DELETE', '/attendance/check-in/$lessonId');
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    await _send(
      'POST',
      '/auth/change-password',
      body: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
  }

  Future<void> logout() async {
    final refreshToken = await _storage.read(key: _refreshTokenKey);
    try {
      if (refreshToken != null) {
        await _send(
          'POST',
          '/auth/logout',
          body: {'refreshToken': refreshToken},
          authenticated: false,
        );
      }
    } finally {
      await clearSession();
    }
  }

  Future<http.Response> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
    bool authenticated = true,
    bool allowRefresh = true,
  }) async {
    final uri =
        Uri.parse('${_apiBaseUrl.replaceFirst(RegExp(r'/$'), '')}$path');
    final headers = <String, String>{'Accept': 'application/json'};
    if (body != null) {
      headers['Content-Type'] = 'application/json';
    }
    if (authenticated) {
      final token = await _storage.read(key: _accessTokenKey);
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    final http.Response response;
    try {
      response = await switch (method) {
        'GET' => _http.get(uri, headers: headers),
        'POST' =>
          _http.post(uri, headers: headers, body: jsonEncode(body ?? {})),
        'PUT' => _http.put(uri, headers: headers, body: jsonEncode(body ?? {})),
        'DELETE' => _http.delete(uri, headers: headers),
        _ =>
          throw ArgumentError.value(method, 'method', 'Metodo HTTP invalido'),
      };
    } on http.ClientException {
      throw const ApiException('Nao foi possivel conectar a API.', 0);
    } on IOException {
      throw const ApiException('Nao foi possivel conectar a API.', 0);
    }

    if (response.statusCode == 401 && authenticated && allowRefresh) {
      final refreshed = await _refreshSession();
      if (refreshed) {
        return _send(
          method,
          path,
          body: body,
          authenticated: authenticated,
          allowRefresh: false,
        );
      }
      await clearSession();
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(_errorMessage(response), response.statusCode);
    }
    return response;
  }

  Future<bool> _refreshSession() async {
    final refreshToken = await _storage.read(key: _refreshTokenKey);
    if (refreshToken == null) {
      return false;
    }

    try {
      final response = await _send(
        'POST',
        '/auth/refresh',
        body: {'refreshToken': refreshToken},
        authenticated: false,
        allowRefresh: false,
      );
      final payload = _decodeObject(response.body);
      final accessToken = payload['accessToken'] as String?;
      final nextRefreshToken = payload['refreshToken'] as String?;
      if (accessToken == null || nextRefreshToken == null) {
        return false;
      }

      await _storage.write(key: _accessTokenKey, value: accessToken);
      await _storage.write(key: _refreshTokenKey, value: nextRefreshToken);
      return true;
    } on ApiException catch (error) {
      if (error.statusCode == 401) {
        return false;
      }
      rethrow;
    }
  }

  Map<String, dynamic> _decodeObject(String body) {
    final decoded = jsonDecode(body);
    if (decoded is! Map<String, dynamic>) {
      throw const ApiException('Resposta inesperada da API.', 500);
    }
    return decoded;
  }

  List<Map<String, dynamic>> _decodeList(String body) {
    final decoded = jsonDecode(body);
    if (decoded is! List<dynamic>) {
      throw const ApiException('Resposta inesperada da API.', 500);
    }
    return decoded.cast<Map<String, dynamic>>();
  }

  String _errorMessage(http.Response response) {
    try {
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic>) {
        final message =
            decoded['detail'] ?? decoded['message'] ?? decoded['title'];
        if (message is String && message.isNotEmpty) {
          return message;
        }
      }
      if (decoded is String && decoded.isNotEmpty) {
        return decoded;
      }
    } on FormatException {
      if (response.body.trim().isNotEmpty) {
        return response.body;
      }
    }
    return 'Nao foi possivel concluir a solicitacao (${response.statusCode}).';
  }
}
