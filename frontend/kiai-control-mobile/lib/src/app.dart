import 'package:flutter/material.dart';

import 'api_client.dart';
import 'models.dart';
import 'password_policy.dart';

class KiaiControlApp extends StatefulWidget {
  const KiaiControlApp({super.key});

  @override
  State<KiaiControlApp> createState() => _KiaiControlAppState();
}

class _KiaiControlAppState extends State<KiaiControlApp> {
  final _api = ApiClient();
  ClientProfile? _profile;
  TeacherProfile? _teacherProfile;
  String? _startupError;
  bool _checkingSession = true;

  @override
  void initState() {
    super.initState();
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    try {
      if (await _api.hasSession) {
        final role = await _api.sessionRole;
        if (role == 'teacher') {
          _teacherProfile = await _api.getTeacherProfile();
        } else if (role == 'client') {
          _profile = await _api.getProfile();
        } else {
          try {
            _profile = await _api.getProfile();
            await _api.saveSessionRole('client');
          } on ApiException catch (error) {
            if (error.statusCode != 403 && error.statusCode != 404) {
              rethrow;
            }
            _teacherProfile = await _api.getTeacherProfile();
            await _api.saveSessionRole('teacher');
          }
        }
      }
    } on ApiException catch (error) {
      if (error.statusCode == 401 ||
          error.statusCode == 403 ||
          error.statusCode == 404) {
        await _api.clearSession();
      } else {
        _startupError = error.message;
      }
    } finally {
      if (mounted) {
        setState(() => _checkingSession = false);
      }
    }
  }

  Future<void> _onSignedIn(String role) async {
    if (role == 'teacher') {
      final profile = await _api.getTeacherProfile();
      if (mounted) {
        setState(() => _teacherProfile = profile);
      }
    } else {
      final profile = await _api.getProfile();
      if (mounted) {
        setState(() => _profile = profile);
      }
    }
  }

  Future<void> _onSignOut() async {
    try {
      await _api.logout();
    } finally {
      if (mounted) {
        setState(() {
          _profile = null;
          _teacherProfile = null;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Kiai Control',
      debugShowCheckedModeBanner: false,
      theme: _buildTheme(),
      home: _checkingSession
          ? const _LoadingScreen()
          : _profile == null
              ? _teacherProfile == null
                  ? _LoginScreen(
                      api: _api,
                      onSignedIn: _onSignedIn,
                      startupError: _startupError,
                    )
                  : _TeacherShell(
                      api: _api,
                      profile: _teacherProfile!,
                      onSignOut: _onSignOut,
                    )
              : _ClientShell(
                  api: _api,
                  profile: _profile!,
                  onSignOut: _onSignOut,
                ),
    );
  }
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
  Widget build(BuildContext context) => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
}

class _LoginScreen extends StatefulWidget {
  const _LoginScreen({
    required this.api,
    required this.onSignedIn,
    this.startupError,
  });

  final ApiClient api;
  final Future<void> Function(String role) onSignedIn;
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
    if (!_formKey.currentState!.validate()) {
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      final role = await widget.api.login(_email.text.trim(), _password.text);
      await widget.onSignedIn(role);
    } on ApiException catch (error) {
      await widget.api.clearSession();
      setState(() => _error = error.message);
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
                      style:
                          Theme.of(context).textTheme.headlineMedium?.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
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
                          icon: Icon(_obscurePassword
                              ? Icons.visibility
                              : Icons.visibility_off),
                        ),
                      ),
                      validator: (value) => value == null || value.isEmpty
                          ? 'Informe sua senha.'
                          : null,
                      onFieldSubmitted: (_) => _submit(),
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 14),
                      Text(
                        _error!,
                        style: TextStyle(
                            color: Theme.of(context).colorScheme.error),
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
                                child:
                                    CircularProgressIndicator(strokeWidth: 2),
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

class _ClientShell extends StatefulWidget {
  const _ClientShell({
    required this.api,
    required this.profile,
    required this.onSignOut,
  });

  final ApiClient api;
  final ClientProfile profile;
  final Future<void> Function() onSignOut;

  @override
  State<_ClientShell> createState() => _ClientShellState();
}

class _ClientShellState extends State<_ClientShell> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    final pages = [
      _HomeTab(
          api: widget.api,
          profile: widget.profile,
          onOpenLessons: () {
            setState(() => _selectedIndex = 1);
          }),
      _LessonsTab(api: widget.api),
      _ProgressTab(api: widget.api),
      _AccountTab(
        api: widget.api,
        profile: widget.profile,
        onSignOut: widget.onSignOut,
      ),
    ];
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(index: _selectedIndex, children: pages),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (index) =>
            setState(() => _selectedIndex = index),
        destinations: const [
          NavigationDestination(
              icon: Icon(Icons.home_outlined), label: 'Inicio'),
          NavigationDestination(
              icon: Icon(Icons.calendar_month), label: 'Aulas'),
          NavigationDestination(
              icon: Icon(Icons.trending_up), label: 'Evolucao'),
          NavigationDestination(
              icon: Icon(Icons.person_outline), label: 'Perfil'),
        ],
      ),
    );
  }
}

class _TeacherShell extends StatefulWidget {
  const _TeacherShell({
    required this.api,
    required this.profile,
    required this.onSignOut,
  });

  final ApiClient api;
  final TeacherProfile profile;
  final Future<void> Function() onSignOut;

  @override
  State<_TeacherShell> createState() => _TeacherShellState();
}

class _TeacherShellState extends State<_TeacherShell> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(
          index: _selectedIndex,
          children: [
            _TeacherLessonsTab(api: widget.api),
            _TeacherAccountTab(
              profile: widget.profile,
              onSignOut: widget.onSignOut,
            ),
          ],
        ),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (index) =>
            setState(() => _selectedIndex = index),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.fact_check_outlined),
            label: 'Chamada',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            label: 'Perfil',
          ),
        ],
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
    await request;
  }

  Future<void> _openLesson(TeacherLesson lesson) async {
    await Navigator.of(context).push<void>(
      MaterialPageRoute(
        builder: (_) => _TeacherAttendanceScreen(
          api: widget.api,
          lesson: lesson,
        ),
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
                  'Selecione uma aula para conferir e validar presenças.'),
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
  const _TeacherAttendanceScreen({
    required this.api,
    required this.lesson,
  });

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
    final clients = await clientsFuture;
    final records = await recordsFuture;
    final recordsByClient = {
      for (final record in records) record.clientId: record,
    };

    _statuses
      ..clear()
      ..addEntries(clients.map((client) {
        final status = recordsByClient[client.clientId]?.status;
        return MapEntry(
          client.clientId,
          _attendanceOptions.containsKey(status)
              ? status!
              : status == 'pending_confirmation'
                  ? 'present'
                  : 'absent',
        );
      }));

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
    await request;
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
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error.message)),
        );
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
      appBar: AppBar(
        title: Text(widget.lesson.modalityName ?? 'Chamada'),
      ),
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
                    final hasCheckIn =
                        roster.pendingCheckIns.contains(client.clientId);
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
                                    style:
                                        Theme.of(context).textTheme.titleMedium,
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
                              decoration:
                                  const InputDecoration(labelText: 'Presenca'),
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
                                        setState(() =>
                                            _statuses[client.clientId] =
                                                status);
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

class _TeacherAccountTab extends StatelessWidget {
  const _TeacherAccountTab({
    required this.profile,
    required this.onSignOut,
  });

  final TeacherProfile profile;
  final Future<void> Function() onSignOut;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Perfil', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        Card(
          child: ListTile(
            leading: const CircleAvatar(child: Icon(Icons.person_outline)),
            title: Text(profile.displayName),
            subtitle: const Text('Professor'),
          ),
        ),
        const SizedBox(height: 16),
        OutlinedButton.icon(
          onPressed: onSignOut,
          icon: const Icon(Icons.logout),
          label: const Text('Sair'),
        ),
      ],
    );
  }
}

class _HomeTab extends StatefulWidget {
  const _HomeTab({
    required this.api,
    required this.profile,
    required this.onOpenLessons,
  });

  final ApiClient api;
  final ClientProfile profile;
  final VoidCallback onOpenLessons;

  @override
  State<_HomeTab> createState() => _HomeTabState();
}

class _HomeTabState extends State<_HomeTab> {
  late Future<(List<ClientLesson>, FitnessOverview)> _overview;

  @override
  void initState() {
    super.initState();
    _overview = _loadOverview();
  }

  Future<(List<ClientLesson>, FitnessOverview)> _loadOverview() async {
    final results = await Future.wait([
      widget.api.getLessons(),
      widget.api.getFitnessOverview(),
    ]);
    return (results[0] as List<ClientLesson>, results[1] as FitnessOverview);
  }

  Future<void> _reloadOverview() async {
    setState(() => _overview = _loadOverview());
    await _overview;
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _reloadOverview,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text('Ola, ${widget.profile.displayName}',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                  )),
          const SizedBox(height: 6),
          const Text('Vamos manter sua evolucao em movimento?'),
          const SizedBox(height: 22),
          FutureBuilder<(List<ClientLesson>, FitnessOverview)>(
            future: _overview,
            builder: (context, snapshot) {
              if (snapshot.hasError) {
                return _ErrorCard(
                  message: snapshot.error.toString(),
                  onRetry: _reloadOverview,
                );
              }
              if (!snapshot.hasData) {
                return const Center(child: CircularProgressIndicator());
              }
              final lessons = snapshot.data!.$1;
              final fitness = snapshot.data!.$2;
              final nextLesson = lessons.isEmpty ? null : lessons.first;
              return Column(
                children: [
                  _SectionCard(
                    title: 'Sua proxima aula',
                    icon: Icons.event_available,
                    child: nextLesson == null
                        ? const Text(
                            'Nenhuma aula programada para os proximos dias.')
                        : Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(nextLesson.modalityName,
                                  style:
                                      Theme.of(context).textTheme.titleLarge),
                              const SizedBox(height: 6),
                              Text(_formatDateTime(nextLesson.startsAt)),
                              if (nextLesson.branchName?.isNotEmpty == true)
                                Text(nextLesson.branchName!),
                              const SizedBox(height: 14),
                              OutlinedButton(
                                onPressed: widget.onOpenLessons,
                                child: const Text('Ver aulas'),
                              ),
                            ],
                          ),
                  ),
                  const SizedBox(height: 14),
                  _SectionCard(
                    title: 'Sua evolucao',
                    icon: Icons.trending_up,
                    child: Row(
                      children: [
                        _Metric(
                            value: '${fitness.activeGoals}',
                            label: 'metas ativas'),
                        const SizedBox(width: 24),
                        _Metric(
                            value: '${fitness.achievedGoals}',
                            label: 'concluidas'),
                      ],
                    ),
                  ),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
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
    await request;
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
            content: Text(cancel
                ? 'Check-in desmarcado com sucesso.'
                : 'Check-in realizado com sucesso.'),
          ),
        );
        await _reload();
      }
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error.message)),
        );
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
              Text('Aulas', style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 14),
              if (lessons.isEmpty)
                const _EmptyCard(
                    message: 'Nao ha aulas disponiveis no periodo.')
              else
                ...lessons.map(
                  (lesson) => Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(lesson.modalityName,
                              style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 6),
                          Text(_formatDateTime(lesson.startsAt)),
                          if (lesson.branchName?.isNotEmpty == true)
                            Text(lesson.branchName!),
                          const SizedBox(height: 10),
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
                                          strokeWidth: 2),
                                    )
                                  : Text(lesson.canCancelCheckIn
                                      ? 'Desmarcar check-in'
                                      : 'Fazer check-in'),
                            )
                          else if (lesson.checkInStatus ==
                              'pending_confirmation')
                            const Text('Check-in registrado')
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
  const _ProgressTab({required this.api});

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
    await request;
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<FitnessOverview>(
      future: _overview,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return _ErrorCard(
              message: snapshot.error.toString(), onRetry: _reload);
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
              Text('Evolucao',
                  style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                      child: _MetricCard(
                          value: '${overview.activeGoals}',
                          label: 'Metas ativas')),
                  const SizedBox(width: 12),
                  Expanded(
                      child: _MetricCard(
                          value: '${overview.achievedGoals}',
                          label: 'Concluidas')),
                ],
              ),
              const SizedBox(height: 18),
              if (overview.goals.isEmpty)
                const _EmptyCard(
                    message:
                        'Suas metas aparecerao aqui quando forem cadastradas.')
              else
                ...overview.goals.map(
                  (goal) => Card(
                    margin: const EdgeInsets.only(bottom: 12),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(goal.title,
                              style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 12),
                          LinearProgressIndicator(value: goal.progress),
                          const SizedBox(height: 8),
                          Text(
                            '${goal.currentValue.toStringAsFixed(0)} / '
                            '${goal.targetValue.toStringAsFixed(0)}'
                            '${goal.unit == null ? '' : ' ${goal.unit}'}',
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

class _AccountTab extends StatefulWidget {
  const _AccountTab({
    required this.api,
    required this.profile,
    required this.onSignOut,
  });

  final ApiClient api;
  final ClientProfile profile;
  final Future<void> Function() onSignOut;

  @override
  State<_AccountTab> createState() => _AccountTabState();
}

class _AccountTabState extends State<_AccountTab> {
  final _currentPassword = TextEditingController();
  final _newPassword = TextEditingController();
  bool _changingPassword = false;

  @override
  void dispose() {
    _currentPassword.dispose();
    _newPassword.dispose();
    super.dispose();
  }

  Future<void> _changePassword() async {
    if (_currentPassword.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Informe a senha atual.')),
      );
      return;
    }
    if (!PasswordPolicy.isValid(_newPassword.text)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text(PasswordPolicy.requirements)),
      );
      return;
    }
    setState(() => _changingPassword = true);
    try {
      await widget.api.changePassword(
        currentPassword: _currentPassword.text,
        newPassword: _newPassword.text,
      );
      _currentPassword.clear();
      _newPassword.clear();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Senha alterada com sucesso.')),
        );
      }
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error.message)),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _changingPassword = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text('Perfil', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 14),
        _SectionCard(
          title: widget.profile.displayName,
          icon: Icons.person_outline,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (widget.profile.email != null) Text(widget.profile.email!),
              if (widget.profile.phone != null) Text(widget.profile.phone!),
              if (widget.profile.joinedAt != null)
                Text('Aluno desde ${_formatDate(widget.profile.joinedAt!)}'),
            ],
          ),
        ),
        const SizedBox(height: 14),
        _SectionCard(
          title: 'Alterar senha',
          icon: Icons.lock_outline,
          child: Column(
            children: [
              TextField(
                controller: _currentPassword,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Senha atual'),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: _newPassword,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Nova senha',
                  helperText: PasswordPolicy.requirements,
                  helperMaxLines: 4,
                ),
              ),
              const SizedBox(height: 12),
              FilledButton.tonal(
                onPressed: _changingPassword ? null : _changePassword,
                child: const Text('Salvar nova senha'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),
        OutlinedButton.icon(
          onPressed: () async {
            try {
              await widget.onSignOut();
            } on ApiException catch (error) {
              if (context.mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text(error.message)),
                );
              }
            }
          },
          icon: const Icon(Icons.logout),
          label: const Text('Sair da conta'),
        ),
      ],
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
                    child: Text(title,
                        style: Theme.of(context).textTheme.titleMedium),
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

class _Metric extends StatelessWidget {
  const _Metric({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(value,
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  )),
          Text(label),
        ],
      );
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: _Metric(value: value, label: label),
        ),
      );
}

class _EmptyCard extends StatelessWidget {
  const _EmptyCard({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Text(message),
        ),
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
                  onPressed: onRetry, child: const Text('Tentar novamente')),
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
