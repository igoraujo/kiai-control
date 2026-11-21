import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:image_picker/image_picker.dart';

import 'mobile_models.dart';
import 'push_service.dart';

import 'api_client.dart';
import 'models.dart';
import 'password_policy.dart';

part 'feature_views.dart';

class KiaiControlApp extends StatefulWidget {
  const KiaiControlApp({super.key, this.api});
  final ApiClient? api;
  @override
  State<KiaiControlApp> createState() => _KiaiControlAppState();
}

class _KiaiControlAppState extends State<KiaiControlApp>
    with WidgetsBindingObserver {
  late final ApiClient _api = widget.api ?? ApiClient();
  StreamSubscription<String>? _expired;
  MobileContext? _session;
  String? _startupError;
  String? _startupFailure;
  bool _checkingSession = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _expired = _api.sessionExpired.listen((message) {
      if (mounted) {
        setState(() {
          _session = null;
          _startupError = message;
          _startupFailure = null;
        });
      }
    });
    _restoreSession();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && _session != null) {
      _restoreSession();
    }
  }

  Future<void> _restoreSession() async {
    try {
      if (await _api.hasSession) _session = await _api.getContext();
      _startupFailure = null;
    } on ApiException catch (error) {
      if (error.statusCode == 401) {
        _session = null;
        _startupError = error.message;
      } else {
        _startupFailure = error.message;
      }
    } finally {
      if (mounted) setState(() => _checkingSession = false);
    }
  }

  Future<void> _onSignedIn() async {
    final session = await _api.getContext();
    if (mounted) {
      setState(() {
        _session = session;
        _startupError = null;
        _startupFailure = null;
      });
    }
  }

  Future<void> _onSignOut() async {
    try {
      await _api.logout();
    } finally {
      if (mounted) {
        setState(() {
          _session = null;
          _startupError = null;
          _startupFailure = null;
        });
      }
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _expired?.cancel();
    if (widget.api == null) _api.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp(
    key: ValueKey(_session ?? 'anonymous'),
    title: 'Kiai Control',
    debugShowCheckedModeBanner: false,
    locale: const Locale('pt', 'BR'),
    supportedLocales: const [Locale('pt', 'BR')],
    localizationsDelegates: GlobalMaterialLocalizations.delegates,
    theme: _buildTheme(),
    home: _checkingSession
        ? const _LoadingScreen()
        : _session != null
        ? _MobileShell(
            key: ValueKey(_session),
            api: _api,
            session: _session!,
            onSignOut: _onSignOut,
            onSessionUpdated: _onSignedIn,
          )
        : _startupFailure != null
        ? Scaffold(
            body: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _ErrorCard(
                  message: _startupFailure!,
                  onRetry: () async {
                    setState(() => _checkingSession = true);
                    await _restoreSession();
                  },
                ),
                TextButton(
                  onPressed: _onSignOut,
                  child: const Text('Voltar ao login'),
                ),
              ],
            ),
          )
        : _LoginScreen(
            api: _api,
            onSignedIn: _onSignedIn,
            startupError: _startupError,
          ),
  );
}

ThemeData _buildTheme() {
  const navy = Color(0xFF142B4A);
  const orange = Color(0xFFEF8B32);
  final scheme = ColorScheme.fromSeed(
    seedColor: navy,
    primary: navy,
    secondary: orange,
    surface: const Color(0xFFF7F8FA),
  );
  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: const Color(0xFFF7F8FA),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
    ),
  );
}

class _LoadingScreen extends StatelessWidget {
  const _LoadingScreen();

  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: CircularProgressIndicator()));
}

class _LoginScreen extends StatefulWidget {
  const _LoginScreen({
    required this.api,
    required this.onSignedIn,
    this.startupError,
  });

  final ApiClient api;
  final Future<void> Function() onSignedIn;
  final String? startupError;

  @override
  State<_LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<_LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _submitting = false;
  bool _obscurePassword = true;
  late String? _error = widget.startupError;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting || !_formKey.currentState!.validate()) {
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await widget.api.login(_email.text.trim(), _password.text);
      await widget.onSignedIn();
    } on ApiException catch (error) {
      await widget.api.clearSession();
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Icon(Icons.sports_martial_arts, size: 56),
                    const SizedBox(height: 16),
                    Text(
                      'Kiai Control',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.headlineMedium
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Entre para acompanhar sua jornada.',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyLarge,
                    ),
                    const SizedBox(height: 32),
                    TextFormField(
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      autofillHints: const [AutofillHints.username],
                      decoration: const InputDecoration(labelText: 'E-mail'),
                      validator: (value) =>
                          value == null || !value.contains('@')
                          ? 'Informe um e-mail valido.'
                          : null,
                    ),
                    const SizedBox(height: 14),
                    TextFormField(
                      controller: _password,
                      obscureText: _obscurePassword,
                      autofillHints: const [AutofillHints.password],
                      decoration: InputDecoration(
                        labelText: 'Senha',
                        suffixIcon: IconButton(
                          onPressed: () => setState(
                            () => _obscurePassword = !_obscurePassword,
                          ),
                          icon: Icon(
                            _obscurePassword
                                ? Icons.visibility
                                : Icons.visibility_off,
                          ),
                        ),
                      ),
                      validator: (value) => value == null || value.isEmpty
                          ? 'Informe sua senha.'
                          : null,
                      onFieldSubmitted: (_) {
                        if (!_submitting) _submit();
                      },
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 14),
                      Text(
                        _error!,
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                    ],
                    const SizedBox(height: 22),
                    FilledButton(
                      onPressed: _submitting ? null : _submit,
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        child: _submitting
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text('Entrar'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _TeacherLessonsTab extends StatefulWidget {
  const _TeacherLessonsTab({required this.api});

  final ApiClient api;

  @override
  State<_TeacherLessonsTab> createState() => _TeacherLessonsTabState();
}

class _TeacherLessonsTabState extends State<_TeacherLessonsTab> {
  late Future<List<TeacherLesson>> _lessons;

  @override
  void initState() {
    super.initState();
    _lessons = widget.api.getTeacherLessons();
  }

  Future<void> _reload() async {
    final request = widget.api.getTeacherLessons();
    setState(() => _lessons = request);
    try {
      await request;
    } on ApiException {
      /* FutureBuilder displays the error. */
    }
  }

  Future<void> _openLesson(TeacherLesson lesson) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) =>
            _TeacherAttendanceScreen(api: widget.api, lesson: lesson),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<TeacherLesson>>(
      future: _lessons,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return _ErrorCard(
            message: snapshot.error.toString(),
            onRetry: _reload,
          );
        }
        if (!snapshot.hasData) {
          return const Center(child: CircularProgressIndicator());
        }

        final lessons = snapshot.data!;
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                'Chamada das aulas',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 8),
              const Text(
                'Selecione uma aula para conferir e validar presenças.',
              ),
              const SizedBox(height: 14),
              if (lessons.isEmpty)
                const _EmptyCard(
                  message: 'Nenhuma aula atribuida neste periodo.',
                )
              else
                ...lessons.map(
                  (lesson) => Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: ListTile(
                      contentPadding: const EdgeInsets.all(16),
                      title: Text(lesson.modalityName ?? 'Aula'),
                      subtitle: Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text(
                          [
                            _formatDateTime(lesson.startsAt),
                            if (lesson.branchName?.isNotEmpty == true)
                              lesson.branchName!,
                          ].join('\n'),
                        ),
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => _openLesson(lesson),
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _TeacherAttendanceScreen extends StatefulWidget {
  const _TeacherAttendanceScreen({required this.api, required this.lesson});

  final ApiClient api;
  final TeacherLesson lesson;

  @override
  State<_TeacherAttendanceScreen> createState() =>
      _TeacherAttendanceScreenState();
}

class _TeacherAttendanceScreenState extends State<_TeacherAttendanceScreen> {
  late Future<_AttendanceRoster> _roster;
  final Map<String, String> _statuses = {};
  bool _saving = false;

  static const _attendanceOptions = <String, String>{
    'present': 'Presente',
    'late': 'Atrasado',
    'absent': 'Ausente',
    'justified_absence': 'Falta justificada',
  };

  @override
  void initState() {
    super.initState();
    _roster = _loadRoster();
  }

  Future<_AttendanceRoster> _loadRoster() async {
    final clientsFuture = widget.api.getEligibleClients(widget.lesson.id);
    final recordsFuture = widget.api.getLessonAttendances(widget.lesson.id);
    final values = await Future.wait([clientsFuture, recordsFuture]);
    final clients = values[0] as List<EligibleClient>;
    final records = values[1] as List<AttendanceRecord>;
    final recordsByClient = {
      for (final record in records) record.clientId: record,
    };

    _statuses
      ..clear()
      ..addEntries(
        clients.map((client) {
          final status = recordsByClient[client.clientId]?.status;
          return MapEntry(
            client.clientId,
            _attendanceOptions.containsKey(status)
                ? status!
                : status == 'pending_confirmation'
                ? 'present'
                : 'absent',
          );
        }),
      );

    return _AttendanceRoster(
      clients: clients,
      pendingCheckIns: records
          .where((record) => record.status == 'pending_confirmation')
          .map((record) => record.clientId)
          .toSet(),
    );
  }

  Future<void> _reload() async {
    final request = _loadRoster();
    setState(() => _roster = request);
    try {
      await request;
    } on ApiException {
      /* FutureBuilder displays the error. */
    }
  }

  Future<void> _save() async {
    if (_statuses.isEmpty || _saving) {
      return;
    }

    setState(() => _saving = true);
    try {
      await widget.api.saveLessonAttendance(widget.lesson.id, _statuses);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Presencas salvas com sucesso.')),
        );
        await _reload();
      }
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.lesson.modalityName ?? 'Chamada')),
      body: FutureBuilder<_AttendanceRoster>(
        future: _roster,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return _ErrorCard(
              message: snapshot.error.toString(),
              onRetry: _reload,
            );
          }
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }

          final roster = snapshot.data!;
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Text(
                  _formatDateTime(widget.lesson.startsAt),
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                if (widget.lesson.branchName?.isNotEmpty == true)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(widget.lesson.branchName!),
                  ),
                const SizedBox(height: 18),
                if (roster.clients.isEmpty)
                  const _EmptyCard(
                    message: 'Nao ha alunos ativos nesta modalidade.',
                  )
                else ...[
                  ...roster.clients.map((client) {
                    final hasCheckIn = roster.pendingCheckIns.contains(
                      client.clientId,
                    );
                    return Card(
                      margin: const EdgeInsets.only(bottom: 10),
                      child: Padding(
                        padding: const EdgeInsets.all(14),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    client.clientName,
                                    style: Theme.of(
                                      context,
                                    ).textTheme.titleMedium,
                                  ),
                                ),
                                if (hasCheckIn)
                                  const Chip(
                                    avatar: Icon(Icons.login, size: 16),
                                    label: Text('Check-in'),
                                  ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            DropdownButtonFormField<String>(
                              initialValue: _statuses[client.clientId],
                              decoration: const InputDecoration(
                                labelText: 'Presenca',
                              ),
                              items: _attendanceOptions.entries
                                  .map(
                                    (option) => DropdownMenuItem(
                                      value: option.key,
                                      child: Text(option.value),
                                    ),
                                  )
                                  .toList(),
                              onChanged: _saving
                                  ? null
                                  : (status) {
                                      if (status != null) {
                                        setState(
                                          () => _statuses[client.clientId] =
                                              status,
                                        );
                                      }
                                    },
                            ),
                          ],
                        ),
                      ),
                    );
                  }),
                  const SizedBox(height: 8),
                  FilledButton.icon(
                    onPressed: _saving ? null : _save,
                    icon: _saving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.check),
                    label: Text(_saving ? 'Salvando...' : 'Salvar chamada'),
                  ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

class _AttendanceRoster {
  const _AttendanceRoster({
    required this.clients,
    required this.pendingCheckIns,
  });

  final List<EligibleClient> clients;
  final Set<String> pendingCheckIns;
}

class _LessonsTab extends StatefulWidget {
  const _LessonsTab({required this.api});

  final ApiClient api;

  @override
  State<_LessonsTab> createState() => _LessonsTabState();
}

class _LessonsTabState extends State<_LessonsTab> {
  late Future<List<ClientLesson>> _lessons;
  final Set<String> _checkingIn = {};

  @override
  void initState() {
    super.initState();
    _lessons = widget.api.getLessons();
  }

  Future<void> _reload() async {
    final request = widget.api.getLessons();
    setState(() => _lessons = request);
    try {
      await request;
    } on ApiException {
      /* FutureBuilder displays the error. */
    }
  }

  Future<void> _checkIn(ClientLesson lesson) async {
    await _updateCheckIn(lesson, cancel: false);
  }

  Future<void> _cancelCheckIn(ClientLesson lesson) async {
    await _updateCheckIn(lesson, cancel: true);
  }

  Future<void> _updateCheckIn(
    ClientLesson lesson, {
    required bool cancel,
  }) async {
    if (cancel ? !lesson.canCancelCheckIn : !lesson.canCheckIn) {
      setState(() {});
      return;
    }

    setState(() => _checkingIn.add(lesson.id));
    try {
      if (cancel) {
        await widget.api.cancelCheckIn(lesson.id);
      } else {
        await widget.api.checkIn(lesson.id);
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              cancel
                  ? 'Check-in desmarcado com sucesso.'
                  : 'Check-in pendente de validação.',
            ),
          ),
        );
        await _reload();
      }
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _checkingIn.remove(lesson.id));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ClientLesson>>(
      future: _lessons,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return _ErrorCard(
            message: snapshot.error.toString(),
            onRetry: _reload,
          );
        }
        if (!snapshot.hasData) {
          return const Center(child: CircularProgressIndicator());
        }
        final lessons = snapshot.data!;
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                'Aulas desta semana',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 14),
              if (lessons.isEmpty)
                const _EmptyCard(
                  message: 'Nao ha aulas disponiveis no periodo.',
                )
              else
                ...lessons.map(
                  (lesson) => Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            lesson.modalityName,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                          const SizedBox(height: 6),
                          Text(_formatDateTime(lesson.startsAt)),
                          if (lesson.branchName?.isNotEmpty == true)
                            Text(lesson.branchName!),
                          const SizedBox(height: 10),
                          if (lesson.checkInStatus != null)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 8),
                              child: Text(
                                _attendanceLabel(lesson.checkInStatus!),
                              ),
                            ),
                          if (lesson.canCheckIn || lesson.canCancelCheckIn)
                            FilledButton.tonal(
                              onPressed: _checkingIn.contains(lesson.id)
                                  ? null
                                  : lesson.canCancelCheckIn
                                  ? () => _cancelCheckIn(lesson)
                                  : () => _checkIn(lesson),
                              child: _checkingIn.contains(lesson.id)
                                  ? const SizedBox(
                                      width: 18,
                                      height: 18,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : Text(
                                      lesson.canCancelCheckIn
                                          ? 'Desmarcar check-in'
                                          : 'Fazer check-in',
                                    ),
                            )
                          else if (lesson.checkInStatus ==
                              'pending_confirmation')
                            const Text('Pendente de validação')
                          else if (lesson.checkInStatus == 'present' ||
                              lesson.checkInStatus == 'late')
                            const Text('Presença confirmada')
                          else if (lesson.checkInStatus == 'absent' ||
                              lesson.checkInStatus == 'justified_absence')
                            const Text('Presença não confirmada')
                          else
                            const Text('Check-in encerrado'),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _ProgressTab extends StatefulWidget {
  const _ProgressTab({super.key, required this.api});

  final ApiClient api;

  @override
  State<_ProgressTab> createState() => _ProgressTabState();
}

class _ProgressTabState extends State<_ProgressTab> {
  late Future<FitnessOverview> _overview;

  @override
  void initState() {
    super.initState();
    _overview = widget.api.getFitnessOverview();
  }

  Future<void> _reload() async {
    final request = widget.api.getFitnessOverview();
    setState(() => _overview = request);
    try {
      await request;
    } on ApiException {
      /* FutureBuilder displays the error. */
    }
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<FitnessOverview>(
      future: _overview,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return _ErrorCard(
            message: snapshot.error.toString(),
            onRetry: _reload,
          );
        }
        if (!snapshot.hasData) {
          return const Center(child: CircularProgressIndicator());
        }
        final overview = snapshot.data!;
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                'Evolucao',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 14),
              _Metrics(
                values: {
                  'Metas ativas': '${overview.activeGoals}',
                  'Concluídas': '${overview.achievedGoals}',
                },
              ),
              const SizedBox(height: 18),
              if (overview.goals.isEmpty)
                const _EmptyCard(
                  message:
                      'Suas metas aparecerao aqui quando forem cadastradas.',
                )
              else
                ...overview.goals.map(
                  (goal) => Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            goal.title,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                          const SizedBox(height: 12),
                          LinearProgressIndicator(value: goal.progress),
                          const SizedBox(height: 8),
                          Text(
                            '${goal.currentValue.toStringAsFixed(0)} / '
                            '${goal.targetValue.toStringAsFixed(0)}'
                            '${goal.unit == null ? '' : ' ${goal.unit}'}',
                          ),
                          if (goal.id != null)
                            TextButton(
                              onPressed: () => Navigator.push(
                                context,
                                MaterialPageRoute<void>(
                                  builder: (_) => _FitnessHistoryPage(
                                    api: widget.api,
                                    goal: goal,
                                  ),
                                ),
                              ),
                              child: const Text('Histórico da meta'),
                            ),
                          if (goal.dueDate != null)
                            Text('Prazo: ${_formatDate(goal.dueDate!)}'),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _SectionCard extends StatelessWidget {
  const _SectionCard({
    required this.title,
    required this.icon,
    required this.child,
  });

  final String title;
  final IconData icon;
  final Widget child;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          child,
        ],
      ),
    ),
  );
}

class _EmptyCard extends StatelessWidget {
  const _EmptyCard({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(padding: const EdgeInsets.all(18), child: Text(message)),
  );
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({required this.message, required this.onRetry});

  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(
            onPressed: onRetry,
            child: const Text('Tentar novamente'),
          ),
        ],
      ),
    ),
  );
}

String _formatDateTime(DateTime value) {
  final date = _formatDate(value);
  final hour = value.hour.toString().padLeft(2, '0');
  final minute = value.minute.toString().padLeft(2, '0');
  return '$date as $hour:$minute';
}

String _formatDate(DateTime value) {
  final day = value.day.toString().padLeft(2, '0');
  final month = value.month.toString().padLeft(2, '0');
  return '$day/$month/${value.year}';
}
