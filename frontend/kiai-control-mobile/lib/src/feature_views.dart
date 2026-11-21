part of 'app.dart';

String _attendanceLabel(String status) => switch (status) {
  'pending_confirmation' => 'Pendente de validação',
  'present' => 'Presença validada',
  'late' => 'Presença validada com atraso',
  'absent' => 'Falta / check-in rejeitado',
  'justified_absence' => 'Falta justificada',
  _ => 'Estado informado pela academia: $status',
};

class _Destination {
  const _Destination(this.id, this.label, this.icon);
  final String id;
  final String label;
  final IconData icon;
}

class _MobileShell extends StatefulWidget {
  const _MobileShell({
    super.key,
    required this.api,
    required this.session,
    required this.onSignOut,
    required this.onSessionUpdated,
  });
  final ApiClient api;
  final MobileContext session;
  final Future<void> Function() onSignOut;
  final Future<void> Function() onSessionUpdated;
  @override
  State<_MobileShell> createState() => _MobileShellState();
}

class _MobileShellState extends State<_MobileShell> {
  String _selected = 'home';
  int _viewRevision = 0;
  late final PushService _push = PushService(widget.api);
  List<_Destination> get _destinations => [
    const _Destination('home', 'Início', Icons.home_outlined),
    if (widget.session.can('student.lessons'))
      const _Destination('lessons', 'Aulas', Icons.calendar_month),
    if (widget.session.can('teacher.lessons'))
      const _Destination('teacher', 'Chamada', Icons.fact_check_outlined),
    if (widget.session.can('attendance.validate'))
      const _Destination(
        'pending',
        'Validar check-ins',
        Icons.how_to_reg_outlined,
      ),
    if (widget.session.can('student.progress'))
      const _Destination('progress', 'Evolução', Icons.trending_up),
    if (widget.session.can('student.progress'))
      const _Destination('ranking', 'Ranking', Icons.leaderboard_outlined),
    if (widget.session.can('notifications.read'))
      const _Destination(
        'notifications',
        'Notificações',
        Icons.notifications_outlined,
      ),
    const _Destination('account', 'Perfil', Icons.person_outline),
  ];

  @override
  void initState() {
    super.initState();
    _push.onOpen = _openNotification;
    _push.onForeground = (id) {
      if (!mounted || !widget.session.can('notifications.read')) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Você recebeu uma notificação.'),
          action: SnackBarAction(
            label: 'Abrir',
            onPressed: () => _openNotification(id),
          ),
        ),
      );
    };
    _push.onError = (message) {
      if (mounted) _message(context, message);
    };
    if (!widget.session.mustChangePassword) _push.start();
  }

  void _openNotification(String id) {
    if (!mounted || !widget.session.can('notifications.read')) return;
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => _NotificationDetail(api: widget.api, id: id),
      ),
    );
  }

  Future<void> _logout() async {
    String? failure;
    _push.dispose();
    try {
      await _push.unregister();
    } catch (e) {
      failure = e.toString();
    }
    _push.dispose();
    try {
      await widget.onSignOut();
    } catch (e) {
      failure = e.toString();
    }
    if (mounted && failure != null) {
      _message(
        context,
        'Sessão local encerrada. Não foi possível revogar todos os registros no servidor: $failure',
      );
    }
  }

  @override
  void dispose() {
    _push.dispose();
    super.dispose();
  }

  Widget _page() => switch (_selected) {
    'lessons' => _LessonsTab(api: widget.api),
    'teacher' => _TeacherLessonsTab(api: widget.api),
    'pending' => _PendingPage(api: widget.api),
    'progress' => _JourneyPage(
      api: widget.api,
      hasGraduation: widget.session.can('student.graduation'),
    ),
    'ranking' => _RankingPage(api: widget.api),
    'notifications' => _NotificationsPage(api: widget.api),
    'account' => _ProfilePage(api: widget.api, push: _push, onSignOut: _logout),
    _ => _DashboardPage(
      api: widget.api,
      session: widget.session,
      navigate: (id) => setState(() => _selected = id),
    ),
  };

  @override
  Widget build(BuildContext context) {
    if (widget.session.mustChangePassword) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Atualize sua senha'),
          actions: [
            IconButton(
              tooltip: 'Sair',
              onPressed: _logout,
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: _PasswordForm(
          api: widget.api,
          requiredChange: true,
          onChanged: widget.onSessionUpdated,
        ),
      );
    }
    final destinations = _destinations;
    final bottom = destinations.length <= 5
        ? destinations
        : [...destinations.take(3), destinations.last];
    final bottomIndex = bottom.indexWhere((d) => d.id == _selected);
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.session.organizationName),
        actions: [
          IconButton(
            tooltip: 'Atualizar',
            onPressed: () => setState(() => _viewRevision++),
            icon: const Icon(Icons.refresh),
          ),
          if (widget.session.can('notifications.read'))
            IconButton(
              tooltip: 'Notificações',
              onPressed: () => setState(() => _selected = 'notifications'),
              icon: const Icon(Icons.notifications_outlined),
            ),
        ],
      ),
      drawer: Drawer(
        child: SafeArea(
          child: ListView(
            children: [
              Padding(
                padding: const EdgeInsets.all(20),
                child: Text(
                  'Kiai Control',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              ...destinations.map(
                (d) => ListTile(
                  leading: Icon(d.icon),
                  title: Text(d.label),
                  selected: _selected == d.id,
                  onTap: () {
                    Navigator.pop(context);
                    setState(() => _selected = d.id);
                  },
                ),
              ),
            ],
          ),
        ),
      ),
      body: SafeArea(
        child: KeyedSubtree(
          key: ValueKey('$_selected-$_viewRevision'),
          child: _page(),
        ),
      ),
      bottomNavigationBar: bottomIndex < 0
          ? null
          : NavigationBar(
              selectedIndex: bottomIndex,
              onDestinationSelected: (index) =>
                  setState(() => _selected = bottom[index].id),
              destinations: bottom
                  .map(
                    (d) => NavigationDestination(
                      icon: Icon(d.icon),
                      label: d.label,
                    ),
                  )
                  .toList(),
            ),
    );
  }
}

void _message(BuildContext context, String text) =>
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

class _AsyncPanel<T> extends StatefulWidget {
  const _AsyncPanel({super.key, required this.load, required this.builder});
  final Future<T> Function() load;
  final Widget Function(T value) builder;
  @override
  State<_AsyncPanel<T>> createState() => _AsyncPanelState<T>();
}

class _AsyncPanelState<T> extends State<_AsyncPanel<T>> {
  late Future<T> _future = widget.load();
  Future<void> _reload() async {
    final future = widget.load();
    setState(() => _future = future);
    try {
      await future;
    } catch (_) {
      /* Rendered by FutureBuilder. */
    }
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<T>(
    future: _future,
    builder: (context, s) {
      if (s.hasError) {
        return _ErrorCard(message: s.error.toString(), onRetry: _reload);
      }
      if (!s.hasData) {
        return const Padding(
          padding: EdgeInsets.all(24),
          child: Center(child: CircularProgressIndicator()),
        );
      }
      return widget.builder(s.data as T);
    },
  );
}

class _DashboardPage extends StatelessWidget {
  const _DashboardPage({
    required this.api,
    required this.session,
    required this.navigate,
  });
  final ApiClient api;
  final MobileContext session;
  final void Function(String id) navigate;
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text(
        'Sua academia, seu ritmo',
        style: Theme.of(context).textTheme.headlineSmall,
      ),
      const SizedBox(height: 6),
      const Text('Acompanhe as informações disponíveis para o seu acesso.'),
      const SizedBox(height: 20),
      if (session.can('reports.dashboard'))
        _AsyncPanel(
          load: api.getDashboard,
          builder: (r) => _SectionCard(
            title: 'Academia neste mês',
            icon: Icons.insights,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Metrics(
                  values: {
                    'Alunos ativos': '${r.activeClients}',
                    'Novos alunos': '${r.newClients}',
                    'Aulas concluídas': '${r.completedLessons}',
                    'Presença': '${r.attendanceRate}%',
                  },
                ),
                const Divider(),
                Text('Receita prevista: ${_money(r.revenueExpected)}'),
                Text('Receita recebida: ${_money(r.revenueReceived)}'),
                Text('Em atraso: ${_money(r.revenueOverdue)}'),
              ],
            ),
          ),
        ),
      if (session.can('student.progress'))
        _AsyncPanel(
          load: api.getProgress,
          builder: (p) => _SectionCard(
            title: 'Sua frequência',
            icon: Icons.local_fire_department_outlined,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Metrics(
                  values: {
                    'Aulas em ${p.year}': '${p.yearClasses}',
                    'Total de aulas': '${p.totalClasses}',
                    'Sequência atual': '${p.currentStreak}',
                  },
                ),
                const SizedBox(height: 12),
                const Text('Presenças validadas em aulas concluídas.'),
                TextButton(
                  onPressed: () => navigate('progress'),
                  child: const Text('Ver evolução'),
                ),
              ],
            ),
          ),
        ),
      if (session.can('teacher.lessons'))
        _AsyncPanel(
          load: api.getTeacherLessons,
          builder: (lessons) => _SectionCard(
            title: 'Suas aulas',
            icon: Icons.sports,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('${lessons.length} aulas no período de 31 dias.'),
                ...lessons
                    .where((l) => l.endsAt.isAfter(DateTime.now()))
                    .take(3)
                    .map(
                      (l) => ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(l.modalityName ?? 'Aula'),
                        subtitle: Text(_formatDateTime(l.startsAt)),
                      ),
                    ),
                TextButton(
                  onPressed: () => navigate('teacher'),
                  child: const Text('Abrir chamada'),
                ),
              ],
            ),
          ),
        ),
      if (session.can('attendance.validate'))
        Card(
          child: ListTile(
            leading: const Icon(Icons.how_to_reg_outlined),
            title: const Text('Check-ins pendentes'),
            subtitle: const Text('Confira e valide a presença dos alunos.'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => navigate('pending'),
          ),
        ),
      if (session.can('student.lessons'))
        _AsyncPanel(
          load: api.getLessons,
          builder: (lessons) => _SectionCard(
            title: 'Aulas desta semana',
            icon: Icons.calendar_month,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Semana da academia: ${session.timezone}. Horários no fuso do aparelho.',
                ),
                if (lessons.isEmpty)
                  const Text('Nenhuma aula disponível nesta semana.'),
                ...lessons
                    .take(3)
                    .map(
                      (lesson) => ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(lesson.modalityName),
                        subtitle: Text(
                          '${_formatDateTime(lesson.startsAt)}${lesson.checkInStatus == null ? '' : '\n${_attendanceLabel(lesson.checkInStatus!)}'}',
                        ),
                      ),
                    ),
                TextButton(
                  onPressed: () => navigate('lessons'),
                  child: const Text('Abrir agenda e fazer check-in'),
                ),
              ],
            ),
          ),
        ),
      if (session.can('notifications.read'))
        _AsyncPanel(
          load: () => api.getNotifications(1),
          builder: (items) => _SectionCard(
            title: 'Notificações recentes',
            icon: Icons.notifications_outlined,
            child: Column(
              children: [
                if (items.isEmpty) const Text('Nenhuma notificação por aqui.'),
                ...items
                    .take(3)
                    .map(
                      (n) => ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(n.title),
                        subtitle: Text(
                          n.body,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute<void>(
                            builder: (_) =>
                                _NotificationDetail(api: api, id: n.id),
                          ),
                        ),
                      ),
                    ),
                TextButton(
                  onPressed: () => navigate('notifications'),
                  child: const Text('Ver todas'),
                ),
              ],
            ),
          ),
        ),
      if (session.capabilities.isEmpty)
        const _EmptyCard(
          message:
              'Seu acesso está ativo. Consulte seu perfil ou fale com a academia para habilitar recursos.',
        ),
    ],
  );
}

String _money(dynamic value) =>
    'R\$ ${(value as num).toStringAsFixed(2).replaceAll('.', ',')}';

class _Metrics extends StatelessWidget {
  const _Metrics({required this.values});
  final Map<String, String> values;
  @override
  Widget build(BuildContext context) => Wrap(
    spacing: 24,
    runSpacing: 16,
    children: values.entries
        .map(
          (e) => SizedBox(
            width: 130,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  e.value,
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                Text(e.key),
              ],
            ),
          ),
        )
        .toList(),
  );
}

class _PendingPage extends StatefulWidget {
  const _PendingPage({required this.api});
  final ApiClient api;
  @override
  State<_PendingPage> createState() => _PendingPageState();
}

class _PendingPageState extends State<_PendingPage> {
  int _page = 1;
  int _revision = 0;
  final Set<String> _busy = {};
  Future<void> _validate(PendingCheckIn item, String status) async {
    if (!_busy.add(item.id)) return;
    setState(() {});
    try {
      await widget.api.validateCheckIn(item.id, status);
      if (mounted) {
        _message(
          context,
          status == 'absent' ? 'Check-in rejeitado.' : 'Presença validada.',
        );
        setState(() => _revision++);
      }
    } catch (e) {
      if (mounted) {
        _message(context, e.toString());
        setState(() => _revision++);
      }
    } finally {
      if (mounted) setState(() => _busy.remove(item.id));
    }
  }

  @override
  Widget build(BuildContext context) => _AsyncPanel(
    key: ValueKey('$_page-$_revision'),
    load: () => widget.api.getPending(_page),
    builder: (items) => ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(
          'Validar check-ins',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const Text(
          'Somente aulas autorizadas pela academia aparecem nesta lista.',
        ),
        const SizedBox(height: 16),
        if (items.isEmpty)
          const _EmptyCard(message: 'Não há check-ins pendentes nesta página.'),
        ...items.map(
          (item) => Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.clientName,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  Text('Check-in em ${_formatDateTime(item.checkedInAt)}'),
                  Text(
                    '${item.modalityName} • ${_formatDateTime(item.lessonStartsAt)}',
                  ),
                  const Text('Pendente de validação'),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      FilledButton(
                        onPressed: _busy.contains(item.id)
                            ? null
                            : () => _validate(item, 'present'),
                        child: const Text('Validar'),
                      ),
                      OutlinedButton(
                        onPressed: _busy.contains(item.id)
                            ? null
                            : () => _validate(item, 'late'),
                        child: const Text('Com atraso'),
                      ),
                      TextButton(
                        onPressed: _busy.contains(item.id)
                            ? null
                            : () => _validate(item, 'absent'),
                        child: const Text('Rejeitar'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
        _Pagination(
          page: _page,
          hasNext: items.length == 25,
          change: (p) => setState(() => _page = p),
        ),
      ],
    ),
  );
}

class _Pagination extends StatelessWidget {
  const _Pagination({
    required this.page,
    required this.hasNext,
    required this.change,
  });
  final int page;
  final bool hasNext;
  final void Function(int) change;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 16),
    child: Wrap(
      spacing: 12,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        OutlinedButton(
          onPressed: page > 1 ? () => change(page - 1) : null,
          child: const Text('Anterior'),
        ),
        Text('Página $page'),
        OutlinedButton(
          onPressed: hasNext ? () => change(page + 1) : null,
          child: const Text('Próxima'),
        ),
      ],
    ),
  );
}

class _JourneyPage extends StatefulWidget {
  const _JourneyPage({required this.api, required this.hasGraduation});
  final ApiClient api;
  final bool hasGraduation;
  @override
  State<_JourneyPage> createState() => _JourneyPageState();
}

class _JourneyPageState extends State<_JourneyPage> {
  int _page = 1;
  int _revision = 0;
  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => setState(() => _revision++),
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(20),
      children: [
        Text('Sua evolução', style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        _AsyncPanel(
          key: ValueKey('progress-$_revision'),
          load: widget.api.getProgress,
          builder: (p) => Column(
            children: [
              _SectionCard(
                title: 'Presenças validadas',
                icon: Icons.insights,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _Metrics(
                      values: {
                        'Total': '${p.totalClasses}',
                        'Ano ${p.year}': '${p.yearClasses}',
                        'Aulas consecutivas': '${p.currentStreak}',
                        'Posição no ano': p.yearPosition == null
                            ? '—'
                            : '${p.yearPosition}º',
                        'Posição total': p.totalPosition == null
                            ? '—'
                            : '${p.totalPosition}º',
                      },
                    ),
                    const SizedBox(height: 12),
                    const Text(
                      'A sequência considera as sessões concluídas das modalidades vinculadas desde sua entrada. Pendências e faltas não contam como presença. Posição indisponível quando o perfil está oculto do ranking.',
                    ),
                    const SizedBox(height: 14),
                    if (p.months.isEmpty)
                      const Text(
                        'Ainda não há presenças validadas nos últimos 12 meses.',
                      ),
                    ...p.months.map((m) {
                      final count = m.count;
                      final maximum = p.months.map((x) => x.count).fold(1, max);
                      return Semantics(
                        label: '${m.month}: $count aulas',
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 5),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Text('${m.month}: $count aulas'),
                              LinearProgressIndicator(
                                value: count / maximum,
                                minHeight: 8,
                              ),
                            ],
                          ),
                        ),
                      );
                    }),
                  ],
                ),
              ),
              if (widget.hasGraduation && p.graduations.isEmpty)
                const _EmptyCard(
                  message:
                      'Nenhuma graduação registrada. As faixas e graus aparecem quando a academia cadastrar sua graduação marcial.',
                ),
              ...p.graduations.map(
                (g) => _SectionCard(
                  title: g.modalityName,
                  icon: Icons.sports_martial_arts,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Faixa ${g.beltName} • ${g.degreeNumber}º grau'),
                      Text(
                        g.nextLevelName == null
                            ? 'Último nível configurado.'
                            : 'Próximo nível: ${g.nextLevelName}',
                      ),
                      if (g.lessonsRemaining != null)
                        Text(
                          'Faltam ${g.lessonsRemaining} aulas validadas para o critério de aulas.',
                        ),
                      const Text(
                        'A promoção depende dos demais critérios e da aprovação da academia.',
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
        _SectionCard(
          title: 'Metas de treino',
          icon: Icons.fitness_center,
          child: SizedBox(
            height: 360,
            child: _ProgressTab(
              key: ValueKey('fitness-$_revision'),
              api: widget.api,
            ),
          ),
        ),
        _AsyncPanel(
          key: ValueKey('journey-$_page-$_revision'),
          load: () => widget.api.getJourney(_page),
          builder: (j) {
            final medals = j.medals;
            final entries = j.attendance;
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _SectionCard(
                  title: 'Conquistas',
                  icon: Icons.emoji_events_outlined,
                  child: Column(
                    children: [
                      if (medals.isEmpty)
                        const Text('Nenhuma conquista registrada ainda.'),
                      ...medals.map(
                        (m) => ListTile(
                          title: Text(m.name),
                          subtitle: Text(
                            m.reason ?? 'Conquista concedida pela academia',
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                _SectionCard(
                  title: 'Histórico de presença',
                  icon: Icons.history,
                  child: Column(
                    children: [
                      if (entries.isEmpty)
                        const Text('Nenhum registro nesta página.'),
                      ...entries.map(
                        (a) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(a.modalityName),
                          subtitle: Text(
                            '${_formatDateTime(a.startsAt)}\n${_attendanceLabel(a.status)}',
                          ),
                        ),
                      ),
                      _Pagination(
                        page: _page,
                        hasNext: _page * 25 < j.attendanceTotal,
                        change: (p) => setState(() => _page = p),
                      ),
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

class _RankingPage extends StatefulWidget {
  const _RankingPage({required this.api});
  final ApiClient api;
  @override
  State<_RankingPage> createState() => _RankingPageState();
}

class _RankingPageState extends State<_RankingPage> {
  bool _year = true;
  int _page = 1;
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text(
        'Ranking de presenças',
        style: Theme.of(context).textTheme.headlineSmall,
      ),
      const Text(
        'Apenas aulas concluídas com presença validada. Empates compartilham a posição.',
      ),
      const SizedBox(height: 16),
      SegmentedButton<bool>(
        segments: const [
          ButtonSegment(value: true, label: Text('Neste ano')),
          ButtonSegment(value: false, label: Text('Total')),
        ],
        selected: {_year},
        onSelectionChanged: (v) => setState(() {
          _year = v.single;
          _page = 1;
        }),
      ),
      _AsyncPanel(
        key: ValueKey('$_year-$_page'),
        load: () => widget.api.getRankings(_year, _page),
        builder: (rows) => Column(
          children: [
            if (rows.isEmpty)
              const _EmptyCard(message: 'Nenhum aluno visível nesta página.'),
            ...rows.map(
              (r) => Card(
                child: ListTile(
                  leading: Text('${r.position}º'),
                  title: Text('${r.clientName}${r.isMe ? ' (você)' : ''}'),
                  subtitle: Text('${r.attendanceCount} aulas validadas'),
                ),
              ),
            ),
            _Pagination(
              page: _page,
              hasNext: rows.length == 25,
              change: (p) => setState(() => _page = p),
            ),
          ],
        ),
      ),
    ],
  );
}

class _NotificationsPage extends StatefulWidget {
  const _NotificationsPage({required this.api});
  final ApiClient api;
  @override
  State<_NotificationsPage> createState() => _NotificationsPageState();
}

class _NotificationsPageState extends State<_NotificationsPage> {
  int _page = 1;
  int _revision = 0;
  @override
  Widget build(BuildContext context) => _AsyncPanel(
    key: ValueKey('$_page-$_revision'),
    load: () => widget.api.getNotifications(_page),
    builder: (items) => RefreshIndicator(
      onRefresh: () async => setState(() => _revision++),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Notificações',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          if (items.isEmpty)
            const _EmptyCard(message: 'Nenhuma notificação nesta página.'),
          ...items.map(
            (n) => Card(
              child: ListTile(
                leading: Icon(
                  n.readAt == null
                      ? Icons.mark_email_unread_outlined
                      : Icons.drafts_outlined,
                ),
                title: Text(n.title),
                subtitle: Text(
                  '${_formatDateTime(n.createdAt)}\n${n.body}',
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                ),
                onTap: () async {
                  await Navigator.push(
                    context,
                    MaterialPageRoute<void>(
                      builder: (_) =>
                          _NotificationDetail(api: widget.api, id: n.id),
                    ),
                  );
                  if (mounted) setState(() => _revision++);
                },
              ),
            ),
          ),
          _Pagination(
            page: _page,
            hasNext: items.length == 25,
            change: (p) => setState(() => _page = p),
          ),
        ],
      ),
    ),
  );
}

class _NotificationDetail extends StatelessWidget {
  const _NotificationDetail({required this.api, required this.id});
  final ApiClient api;
  final String id;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Notificação')),
    body: _AsyncPanel(
      load: () async {
        final data = await api.requestObject('GET', '/notifications/$id');
        final notification = AppNotification.fromJson(data);
        await api.markNotificationRead(id);
        return notification;
      },
      builder: (n) => ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Text(n.title, style: Theme.of(context).textTheme.headlineSmall),
          Text(_formatDateTime(n.createdAt)),
          const SizedBox(height: 20),
          SelectableText(n.body),
        ],
      ),
    ),
  );
}

class _ProfilePage extends StatefulWidget {
  const _ProfilePage({
    required this.api,
    required this.push,
    required this.onSignOut,
  });
  final ApiClient api;
  final PushService push;
  final Future<void> Function() onSignOut;
  @override
  State<_ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<_ProfilePage> {
  int _revision = 0;
  @override
  Widget build(BuildContext context) => _AsyncPanel(
    key: ValueKey(_revision),
    load: widget.api.getAccount,
    builder: (account) => _AccountEditor(
      api: widget.api,
      push: widget.push,
      account: account,
      onSaved: () => setState(() => _revision++),
      onSignOut: widget.onSignOut,
    ),
  );
}

class _AccountEditor extends StatefulWidget {
  const _AccountEditor({
    required this.api,
    required this.push,
    required this.account,
    required this.onSaved,
    required this.onSignOut,
  });
  final ApiClient api;
  final PushService push;
  final MobileAccount account;
  final VoidCallback onSaved;
  final Future<void> Function() onSignOut;
  @override
  State<_AccountEditor> createState() => _AccountEditorState();
}

class _AccountEditorState extends State<_AccountEditor> {
  final _form = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.account.fullName);
  late final _preferred = TextEditingController(
    text: widget.account.preferredName,
  );
  late final _email = TextEditingController(text: widget.account.email);
  late final _phone = TextEditingController(text: widget.account.phone);
  late bool _hide = widget.account.hideFromRanking;
  late bool _pushEnabled = widget.account.pushEnabled;
  bool _busy = false;
  @override
  void dispose() {
    _name.dispose();
    _preferred.dispose();
    _email.dispose();
    _phone.dispose();
    super.dispose();
  }

  Future<void> _perform(Future<void> Function() action, String success) async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      await action();
      if (mounted) {
        _message(context, success);
        widget.onSaved();
      }
    } catch (e) {
      if (mounted) _message(context, e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    await _perform(() async {
      if (_pushEnabled && widget.push.available) {
        await widget.push.sync(requestPermission: true);
      }
      await widget.api.saveAccount({
        'fullName': _name.text.trim(),
        'preferredName': _preferred.text.trim(),
        'email': _email.text.trim().isEmpty ? null : _email.text.trim(),
        'phone': _phone.text.trim(),
        'hideFromRanking': _hide,
        'pushEnabled': _pushEnabled,
      });
      if (!_pushEnabled) await widget.push.unregister();
    }, 'Perfil e preferências salvos.');
  }

  Future<void> _photo() async {
    if (_busy) return;
    try {
      final image = await ImagePicker().pickImage(
        source: ImageSource.gallery,
        maxWidth: 800,
        maxHeight: 800,
        imageQuality: 80,
      );
      if (image == null || !mounted) return;
      await _perform(() async {
        final bytes = await image.readAsBytes();
        if (bytes.length > 512 * 1024) {
          throw const ApiException('Escolha uma foto de até 512 KB.', 0);
        }
        final png = bytes.length >= 8 && bytes[0] == 137 && bytes[1] == 80;
        await widget.api.savePhoto(bytes, png ? 'image/png' : 'image/jpeg');
      }, 'Foto atualizada.');
    } catch (e) {
      if (mounted) _message(context, e.toString());
    }
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text('Meu perfil', style: Theme.of(context).textTheme.headlineSmall),
      const SizedBox(height: 16),
      Center(
        child: CircleAvatar(
          radius: 48,
          backgroundImage: widget.account.photo == null
              ? null
              : MemoryImage(widget.account.photo!),
          child: widget.account.photo == null
              ? const Icon(Icons.person, size: 48)
              : null,
        ),
      ),
      Wrap(
        alignment: WrapAlignment.center,
        children: [
          TextButton.icon(
            onPressed: _busy ? null : _photo,
            icon: const Icon(Icons.add_a_photo_outlined),
            label: const Text('Alterar foto'),
          ),
          if (widget.account.photo != null)
            TextButton(
              onPressed: _busy
                  ? null
                  : () => _perform(widget.api.removePhoto, 'Foto removida.'),
              child: const Text('Remover foto'),
            ),
        ],
      ),
      Form(
        key: _form,
        child: Column(
          children: [
            TextFormField(
              controller: _name,
              maxLength: 160,
              decoration: const InputDecoration(labelText: 'Nome completo'),
              validator: (v) =>
                  v?.trim().isEmpty != false ? 'Informe seu nome.' : null,
            ),
            TextFormField(
              controller: _preferred,
              maxLength: 100,
              decoration: const InputDecoration(
                labelText: 'Como prefere ser chamado(a)',
              ),
            ),
            TextFormField(
              controller: _email,
              maxLength: 160,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(
                labelText: 'E-mail de contato',
                helperText: 'O e-mail de acesso é gerenciado pela academia.',
              ),
              validator: (v) =>
                  v != null &&
                      v.isNotEmpty &&
                      !RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(v.trim())
                  ? 'Informe um e-mail válido.'
                  : null,
            ),
            TextFormField(
              controller: _phone,
              maxLength: 30,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Telefone'),
            ),
          ],
        ),
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        title: const Text('Ocultar meu nome do ranking'),
        value: _hide,
        onChanged: _busy ? null : (v) => setState(() => _hide = v),
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        title: const Text('Receber notificações push'),
        subtitle: Text(widget.push.status),
        value: _pushEnabled,
        onChanged: _busy || !widget.push.available
            ? null
            : (v) => setState(() => _pushEnabled = v),
      ),
      FilledButton(
        onPressed: _busy ? null : _save,
        child: Text(_busy ? 'Salvando…' : 'Salvar perfil e preferências'),
      ),
      const SizedBox(height: 16),
      OutlinedButton.icon(
        onPressed: () => Navigator.push(
          context,
          MaterialPageRoute<void>(
            builder: (_) => Scaffold(
              appBar: AppBar(title: const Text('Alterar senha')),
              body: _PasswordForm(
                api: widget.api,
                onChanged: () async {
                  Navigator.pop(context);
                },
              ),
            ),
          ),
        ),
        icon: const Icon(Icons.lock_outline),
        label: const Text('Alterar senha'),
      ),
      OutlinedButton.icon(
        onPressed: _busy ? null : widget.onSignOut,
        icon: const Icon(Icons.logout),
        label: const Text('Sair da conta'),
      ),
    ],
  );
}

class _PasswordForm extends StatefulWidget {
  const _PasswordForm({
    required this.api,
    required this.onChanged,
    this.requiredChange = false,
  });
  final ApiClient api;
  final Future<void> Function() onChanged;
  final bool requiredChange;
  @override
  State<_PasswordForm> createState() => _PasswordFormState();
}

class _PasswordFormState extends State<_PasswordForm> {
  final _form = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _next = TextEditingController();
  final _confirmation = TextEditingController();
  bool _busy = false;
  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    _confirmation.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_busy || !_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      await widget.api.changePassword(
        currentPassword: _current.text,
        newPassword: _next.text,
      );
      await widget.api.clearSession(
        notify: true,
        message: 'Senha alterada. Entre novamente com a nova senha.',
      );
    } catch (e) {
      if (mounted) _message(context, e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => SingleChildScrollView(
    padding: const EdgeInsets.all(24),
    child: Form(
      key: _form,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (widget.requiredChange)
            const Text('Defina uma senha pessoal para continuar.'),
          const SizedBox(height: 16),
          TextFormField(
            controller: _current,
            obscureText: true,
            decoration: const InputDecoration(labelText: 'Senha atual'),
            validator: (v) =>
                v?.isNotEmpty == true ? null : 'Informe sua senha atual.',
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _next,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Nova senha',
              helperText: PasswordPolicy.requirements,
              helperMaxLines: 4,
            ),
            validator: (v) => PasswordPolicy.isValid(v ?? '')
                ? null
                : PasswordPolicy.requirements,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _confirmation,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Confirme a nova senha',
            ),
            validator: (v) =>
                v == _next.text ? null : 'As senhas não coincidem.',
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _busy ? null : _save,
            child: Text(_busy ? 'Salvando…' : 'Salvar nova senha'),
          ),
        ],
      ),
    ),
  );
}

class _FitnessHistoryPage extends StatefulWidget {
  const _FitnessHistoryPage({required this.api, required this.goal});
  final ApiClient api;
  final FitnessGoal goal;
  @override
  State<_FitnessHistoryPage> createState() => _FitnessHistoryPageState();
}

class _FitnessHistoryPageState extends State<_FitnessHistoryPage> {
  int _page = 1;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.goal.title)),
    body: _AsyncPanel(
      key: ValueKey(_page),
      load: () => widget.api.getGoalProgress(widget.goal.id!, _page),
      builder: (records) => ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Histórico de progresso',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          if (records.isEmpty)
            const _EmptyCard(
              message: 'Nenhuma medição registrada nesta página.',
            ),
          ...records.map(
            (r) => Card(
              child: ListTile(
                title: Text(
                  '${r.value}${widget.goal.unit == null ? '' : ' ${widget.goal.unit}'}',
                ),
                subtitle: Text(
                  '${_formatDateTime(r.recordedAt)}${r.notes == null ? '' : '\n${r.notes}'}',
                ),
              ),
            ),
          ),
          _Pagination(
            page: _page,
            hasNext: records.length == 25,
            change: (p) => setState(() => _page = p),
          ),
        ],
      ),
    ),
  );
}
