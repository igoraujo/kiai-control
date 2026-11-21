import 'package:flutter/material.dart';

import 'src/app.dart';
import 'src/push_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializePush();
  runApp(const KiaiControlApp());
}
