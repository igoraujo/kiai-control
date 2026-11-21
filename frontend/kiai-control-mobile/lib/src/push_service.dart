import 'dart:async';
import 'dart:io';
import 'dart:math';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';

import 'api_client.dart';

const _firebaseOptions = FirebaseOptions(
  apiKey: String.fromEnvironment('FIREBASE_API_KEY'),
  appId: String.fromEnvironment('FIREBASE_APP_ID'),
  messagingSenderId: String.fromEnvironment('FIREBASE_SENDER_ID'),
  projectId: String.fromEnvironment('FIREBASE_PROJECT_ID'),
  iosBundleId: String.fromEnvironment('FIREBASE_IOS_BUNDLE_ID'),
);

@pragma('vm:entry-point')
Future<void> firebaseBackgroundMessage(RemoteMessage message) async {
  if (Firebase.apps.isEmpty) {
    await Firebase.initializeApp(options: _firebaseOptions);
  }
  // Android/APNs display notification payloads. The main isolate handles taps.
}

String? pushConfigurationError;
RemoteMessage? _initialPush;
Future<void> initializePush() async {
  if (!const bool.fromEnvironment('PUSH_ENABLED')) return;
  try {
    if (Firebase.apps.isEmpty) {
      await Firebase.initializeApp(options: _firebaseOptions);
    }
    _initialPush ??= await FirebaseMessaging.instance.getInitialMessage();
    FirebaseMessaging.onBackgroundMessage(firebaseBackgroundMessage);
    await FirebaseMessaging.instance
        .setForegroundNotificationPresentationOptions(
          alert: false,
          badge: false,
          sound: false,
        );
  } catch (_) {
    pushConfigurationError =
        'Não foi possível inicializar as notificações neste dispositivo.';
  }
}

class PushService {
  PushService(this.api);
  final ApiClient api;
  final _storage = SecureSessionStorage();
  final List<StreamSubscription<dynamic>> _subscriptions = [];
  bool get available =>
      const bool.fromEnvironment('PUSH_ENABLED') &&
      Firebase.apps.isNotEmpty &&
      pushConfigurationError == null;
  String get status =>
      pushConfigurationError ??
      (available
          ? 'Receba avisos da academia neste aparelho.'
          : 'Push não configurado nesta instalação.');
  bool _active = true;
  void Function(String id)? onOpen;
  void Function(String id)? onForeground;
  void Function(String message)? onError;

  Future<String> _installation() async {
    var id = await _storage.read('installation_id');
    if (id != null) return id;
    final random = Random.secure();
    id = List.generate(
      32,
      (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0'),
    ).join();
    await _storage.write('installation_id', id);
    return id;
  }

  String? _id(RemoteMessage message) {
    final id = message.data['notificationId'];
    return id is String && RegExp(r'^[0-9a-fA-F-]{36}$').hasMatch(id)
        ? id
        : null;
  }

  Future<void> start() async {
    if (!available) return;
    _subscriptions.add(
      FirebaseMessaging.onMessage.listen((m) {
        final id = _id(m);
        if (_active && id != null) onForeground?.call(id);
      }),
    );
    _subscriptions.add(
      FirebaseMessaging.onMessageOpenedApp.listen((m) {
        final id = _id(m);
        if (_active && id != null) onOpen?.call(id);
      }),
    );
    _subscriptions.add(
      FirebaseMessaging.instance.onRegistered.listen((token) async {
        try {
          final account = await api.getAccount();
          if (_active && account.pushEnabled) await _register(token);
        } catch (e) {
          if (_active) onError?.call(e.toString());
        }
      }),
    );
    try {
      final initial =
          _initialPush ?? await FirebaseMessaging.instance.getInitialMessage();
      _initialPush = null;
      final id = initial == null ? null : _id(initial);
      if (_active && id != null) onOpen?.call(id);
      final account = await api.getAccount();
      if (_active && account.pushEnabled) await sync(requestPermission: false);
    } catch (e) {
      if (_active) onError?.call(e.toString());
    }
  }

  Future<void> _register(String token) async {
    if (!_active) return;
    await api.registerDevice(
      await _installation(),
      token,
      Platform.isIOS ? 'ios' : 'android',
    );
  }

  Future<void> sync({required bool requestPermission}) async {
    if (!available) throw ApiException(status, 0);
    final settings = requestPermission
        ? await FirebaseMessaging.instance.requestPermission(
            alert: true,
            badge: true,
            sound: true,
          )
        : await FirebaseMessaging.instance.getNotificationSettings();
    if (settings.authorizationStatus != AuthorizationStatus.authorized &&
        settings.authorizationStatus != AuthorizationStatus.provisional) {
      throw const ApiException(
        'Permita notificações nas configurações do aparelho para receber push.',
        0,
      );
    }
    if (Platform.isIOS &&
        await FirebaseMessaging.instance.getAPNSToken() == null) {
      throw const ApiException(
        'O registro Apple ainda não está disponível. Tente novamente em instantes.',
        0,
      );
    }
    final registration = FirebaseMessaging.instance.onRegistered.first.timeout(
      const Duration(seconds: 25),
    );
    final values = await Future.wait([
      registration,
      FirebaseMessaging.instance.register().then((_) => ''),
    ]);
    final target = values.first;
    if (target.isEmpty) {
      throw const ApiException('Não foi possível registrar o dispositivo.', 0);
    }
    await _register(target);
  }

  Future<void> unregister() async {
    final id = await _storage.read('installation_id');
    try {
      if (id != null) await api.removeDevice(id);
    } finally {
      if (available) await FirebaseMessaging.instance.unregister();
    }
  }

  void dispose() {
    _active = false;
    for (final subscription in _subscriptions) {
      subscription.cancel();
    }
    _subscriptions.clear();
  }
}
