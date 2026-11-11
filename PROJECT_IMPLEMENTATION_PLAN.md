# Kiai Control - Plano Geral de Implementacao

**Estado verificado em:** 2026-11-11
**Escopo:** API, painel Admin, banco de dados, testes, seguranca, operacao e mobile.  
**Objetivo:** ser a fonte de planejamento de evolucao do projeto e a checklist compartilhada pelos agentes.

## Como Usar Este Documento

- O codigo, os testes executados e as migrations sao a evidencia do estado atual. `SYSTEM_DESIGN.md` descreve principalmente o estado desejado, nao comprova implementacao.
- `Presente` significa que existe uma implementacao identificavel no repositorio. Nao significa que o fluxo foi validado em ambiente real.
- Marque uma tarefa como concluida somente depois de executar e registrar a validacao definida no item e seus criterios de aceite.
- Antes de criar endpoint, tela, tabela ou teste, procure primeiro a implementacao existente. Evolua-a; nao duplique funcionalidades ja presentes.
- Ao concluir uma tarefa, atualize este checklist, a evidencia/validacao e a data. Registre bloqueios sem marcar itens como concluidos.
- Nao use os checklists arquivados em `API_IMPLEMENTATION_PLAN.md` ou o snapshot de `CHECKPOINT.md` como estado atual.

## Resumo do Estado

O backend ja tem uma base ampla de API e modulos de negocio. O Admin cobre uma parte dos fluxos operacionais. O banco tem schema inicial, migrations `001`-`016` e script de bootstrap com ledger para evitar reaplicacao. A principal lacuna nao e a ausencia de todos os modulos: e a falta de verificacao completa dos fluxos contra PostgreSQL, de cobertura automatizada em algumas areas e de paridade do painel com as capacidades existentes da API.

O workspace contem API, Admin e um aplicativo Flutter inicial. O Admin recebeu as jornadas de equipe/contas e portal pessoal, com diferenciacao visual Gym/Fight. Infraestrutura/deploy e observabilidade de producao continuam sendo etapas operacionais separadas; a validacao local nao comprova deploy ou servicos externos.

### Administração da plataforma (validada em 2026-11-11)

- [x] Separar a área `platform_owner` da operação das academias, com menu e identificação visual próprios; as rotas globais não aceitam papéis locais nem troca de tenant por header.
- [x] Listar academias com busca literal parametrizada, status e paginação; cadastrar academia e primeiro owner; editar dados e ativar/suspender/inativar com motivo auditado.
- [x] Impedir login/refresh e uso de JWT de academias suspensas/inativas; revogar sessões e recuperação na mudança de status, preservando as demais academias.
- [x] Cadastrar responsáveis, ajustar e-mail/perfis/estado, bloquear/reativar, revogar sessões e definir senha temporária com troca obrigatória; proteger a conta global, vínculos de aluno/professor e o último owner ativo sob concorrência.
- [x] Persistir auditoria de mutações da plataforma na mesma transação da operação, incluindo cadastro e bootstrap; testar rollback quando a gravação da auditoria falha.
- [x] Consultar auditorias globais com academia, usuário, ação e período; registrar acessos globais e mostrar detalhes legíveis sem senhas/hashes.
- [x] Persistir ocorrências HTTP 5xx, login 401 e falhas do middleware de auditoria, com template de rota e correlação; consultar indicadores de 24h e histórico paginado. Não são armazenados corpo, query string ou credenciais.
- [x] Adicionar migration `015`, índices, schema consolidado idempotente, DBML e documentação em `PLATFORM_ADMINISTRATION.md`; migration aplicada no banco de testes com ledger, sem excluir registros.

Validação executada: **81 unitários da API, 39 de integração PostgreSQL, 2 arquiteturais, 41 unitários/componentes Admin e 15 E2E Chromium/API/Admin/PostgreSQL: 178 aprovados**. Os novos cenários cobrem cadastro e edição de academia, novo owner, senha temporária, revogação de JWT/refresh, suspensão/reativação, auditoria global, bloqueio dos papéis locais, correlação e telas em 390 px. O schema consolidado foi criado e reaplicado em banco descartável. O E2E também identificou e permitiu corrigir a atualização dos campos que habilitam o envio de comunicados.

O provisionamento pessoal foi realizado na base de testes, com hash validado, senha temporária em arquivo privado fora do repositório e auditoria transacional. Nenhuma credencial de acesso foi adicionada ao código. O módulo não elimina as pendências de readiness, monitoração externa nem a auditoria transacional nas demais áreas.

### Auditoria por academia, ciclo de vida e privacidade (validada em 2026-11-11)

- [x] Auditoria em `/platform/organizations/{id}/audit`, com academia fixa, filtros e histórico de academias arquivadas.
- [x] Visões Em operação/Arquivadas/Todas; arquivamento por `deleted_at` com confirmação do nome e motivo; revogação de sessões; restauração como inativa; nenhuma exclusão física em cascata de cadastros.
- [x] Listagem de auditoria sem identidade completa, IP, alvo ou metadados pessoais; detalhes exigem autorização e auditoria bem-sucedida, sem justificativa manual; metadados usam lista de chaves permitidas; o texto do filtro de autor não é copiado para o log.
- [x] Dashboard por academia e período: tráfego, 4xx/5xx, média ponderada/máximo de duração, gráfico diário interativo, templates de rotas, contadores de contas e preservação; sem dados individuais de alunos ou saúde.
- [x] Readiness com probes de PostgreSQL/cache, timeout e descrição sem segredos; E2E interrompe PostgreSQL e confirma readiness 503/liveness 200.
- [x] Retenção configurável de telemetria e remoção de identificadores/metadados antigos da auditoria; preservação legal com fundamento auditado; locks serializam limpeza e alterações de preservação; cadastros e financeiro são preservados.
- [x] Migration `016`, schema consolidado idempotente e DBML com 39 tabelas; aplicada na base de testes com ledger. Política técnica e limites documentados em `PLATFORM_ADMINISTRATION.md`, com referências oficiais da ANPD.

Validação executada: **85 unitários da API, 44 de integração PostgreSQL, 2 arquiteturais, 41 unitários/componentes Admin e 17 E2E: 189 aprovados**. Inclui isolamento por academia, privacidade da resposta, confirmação/rollback de arquivamento, restauração sem reativação, retenção/preservação, agregação ponderada, captura de templates sem query string e métricas persistidas pelo coletor real. O navegador percorreu os novos controles e a versão móvel; a atualização dos indicadores também foi verificada.

Os padrões de 30/90/180 dias são opções técnicas a ajustar/documentar pelo controlador segundo finalidade, base legal e obrigações de conservação. Não representam prazo legal universal nem certificação de conformidade. Arquivamento não é eliminação de dados pessoais; direitos dos titulares também envolvem cadastros, backups e logs externos. Redis real e staging exigem validação específica; o E2E desta entrega aferiu PostgreSQL real e cache em memória.

### Retomada de contrato no mesmo cadastro (validada em 2026-11-11)

- [x] Ação **Restaurar e reativar** para academias arquivadas, com confirmação do nome e motivo, reutilizando o mesmo ID e os cadastros existentes.
- [x] `POST /platform/organizations/{id}/restore` aceita `reactivate: true` e restaura/ativa na mesma transação da auditoria `platform.organization.reactivated`; sem a opção, a restauração continua como inativa.
- [x] Preservar usuários, alunos, credenciais e histórico; manter contas bloqueadas bloqueadas; exigir novo login, sem recuperar JWT/refresh tokens antigos. Reativar não recria dados anteriormente eliminados por outra política.
- [x] Testar retomada após cancelamento, ausência de duplicação, preservação dos vínculos e rollback se a auditoria da reativação falhar. Nenhuma migration nova foi necessária.

Validação: **85 unitários API, 45 de integração PostgreSQL, 2 arquiteturais, 41 unitários/componentes Admin e 18 E2E: 191 testes distintos aprovados**. Na execução geral, um teste antigo de comunicados falhou esperando o botão habilitar; a seleção passou a aguardar confirmação visual e o preenchimento passou a encerrar o foco antes de enviar. A reexecução desse cenário passou. O novo E2E confirmou retorno de login e acesso ao mesmo aluno, usuários e academia sem novo cadastro.

### Consulta direta dos detalhes de auditoria (validada em 2026-11-11)

- [x] Remover a justificativa manual da tela e da API, conforme solicitado. **Ver detalhes** carrega o evento automaticamente, com indicação de carregamento e nova tentativa em caso de erro.
- [x] Preservar autorização `platform_owner`, vínculo entre evento/academia, filtros de dados sensíveis e registro automático `platform.audit.details_viewed` antes de entregar os detalhes.

Validação desta alteração: **1 teste de integração PostgreSQL e 1 E2E aprovados**. A integração verifica consulta sem justificativa, IDs obrigatórios, isolamento por academia e metadados filtrados. O navegador confirma abertura sem formulário/confirmação adicional e registro automático de cada consulta. API e Admin foram compilados por esses testes; `git diff --check` aprovado. Nenhuma migration ou commit foi necessário.

### Serviço agnóstico de e-mail (validado em 2026-11-11)

- [x] Contrato `IEmailSender` e mensagem/resultado no Core, sem dependência de fornecedor; composição da recuperação separada do transporte.
- [x] Adaptadores Disabled e SMTP com MailKit: texto/HTML, nomes, remetente/Reply-To globais ou por mensagem, autenticação, STARTTLS obrigatório/TLS desde a conexão, timeout, cancelamento e resultado Accepted/Disabled/Failed.
- [x] Provedor selecionável por configuração e extensão `AddEmailProvider<TSender>` para API HTTP/SDK próprio, sem alterar casos de uso; validação de startup, comparação de nomes sem diferença de caixa e descarte único de adaptadores com recursos.
- [x] Recuperação usa o serviço compartilhado, token no fragmento, resposta genérica, consumo único e revogação de sessões. HTTPS obrigatório fora do loopback em Development; compatibilidade de configuração com as chaves antigas `Smtp:*`.
- [x] Configuração/modelo sem credenciais, captura local em `compose.email.yml` e documentação completa em `EMAIL_CONFIGURATION.md`. Nenhuma migration foi necessária.

Validação: **113 unitários API, 51 de integração (PostgreSQL/SMTP real em Mailpit), 2 arquiteturais, 41 unitários/componentes Admin e 19 E2E: 226 testes distintos aprovados**. Os 28 testes unitários novos cobrem configuração, adaptador sem SMTP, escopo/descarte, URL/token, entradas inválidas e cancelamento. Os seis de SMTP verificam MIME Unicode/texto/HTML, autenticação, remetente/resposta, falha sem dados pessoais nos logs e ausência de downgrade TLS. O novo E2E recebe o e-mail pelo SMTP e usa seu token para redefinir a senha, revogar sessões anteriores e rejeitar reutilização.

As passadas completas do Admin expuseram sincronização em dois E2E antigos (seleção de aluno/metas e tipo de modalidade). As seleções passaram a aguardar a confirmação visual e o preenchimento da meta encerra o foco; a reexecução conjunta dos dois cenários e do novo E2E de e-mail passou. `git diff --check`, JSONs e `docker compose config --quiet` aprovados.

O envio começa desabilitado se não houver seleção/configuração de provedor, respeitando instalações com configuração SMTP antiga. Não foram cadastradas credenciais de provedor externo nem enviados e-mails a pessoas reais. Aceitação SMTP não garante entrega na caixa de entrada; fila durável/retry automático e webhooks de entrega/bounce não fazem parte desta implementação.

### Fluxo de aulas

O fluxo e modalidade → horarios recorrentes por unidade (professor opcional) → aulas por data → chamada. Um aluno ativo vinculado a modalidade pode frequentar qualquer horario dela; nao ha vinculo fixo a horario. A migration `007_remove_class_groups.sql` remove tabelas, endpoints e telas de turma/matricula fixa; aulas e ciclos de ranking sao associados a modalidade e matriculas ativas sao transferidas para `person_modalities`.

### Atualizacao de Implementacao (2026-10-31)

Esta atualizacao prevalece sobre itens de status conflitantes nas tabelas abaixo:

- JWT HS256 valida issuer, audience, assinatura e expiracao; a chave de ao menos 32 bytes e obrigatoria fora de Development. Fallback de desenvolvimento foi removido do `appsettings.json`.
- Claims de permissions sao carregadas no login/refresh e exigidas por policies nos controllers, em conjunto com as roles existentes.
- Migrations `004` e `005` definem grants globais por role e overrides por organizacao; os endpoints de alteracao usam tenant/ator do token, e a role `owner` nao pode perder suas permissions pela API.
- Login, refresh, logout e operacoes de senha usam limite de 10 requisicoes por IP/minuto. Recuperacao nao expoe token e retorna mensagem generica; envio por e-mail continua pendente.
- Admin tem paginas `/access` e `/billing`, navega por permissions, oferece vinculo de modalidades no formulario do aluno e apresenta a jornada consolidada do aluno; em 2026-10-31 passaram build, 23 testes unitarios e 2 fluxos E2E locais com Playwright, PostgreSQL, API e Admin, incluindo bootstrap em banco vazio e historico financeiro do aluno.
- Na validacao da remocao do modelo de turmas, a fixture PostgreSQL executa as migrations `001`-`008`; os testes verificam ausencia das tabelas e campos de turma, alem do fluxo aberto de aula. No GitHub Actions run `41257017649`, em 2026-10-31, passaram 48 testes unitarios, 6 de integracao PostgreSQL, 1 arquitetural e 15 do Admin; build API/Admin, verificacao de TRX e upload do artifact tambem passaram. Em 2026-10-31 o bootstrap aplicou as migrations `002`-`007` e o seed no banco local `dev`; a tabela `modality_schedules` existe e as tabelas de turma nao. Deploy e servicos externos nao foram validados.
- O E2E cobre o bootstrap real com `create_database.sh` em banco vazio, reaplica o script para validar idempotencia, confere as migrations/schema/seed e autentica o admin no navegador; tambem percorre criacao/edicao de aluno, vinculo de modalidade, geracao de aula, check-in, assinatura, fatura e pagamento persistido e confirma que a jornada do aluno reune cadastro, modalidades e financeiro. Os 2 cenarios locais passaram em 2026-10-31. O workflow instala Chromium, executa o projeto e exige seu TRX; a validacao remota dessa nova etapa de CI ainda esta pendente.
- A jornada do professor em `/teachers` permite associar/desassociar horarios recorrentes por modalidade e unidade, consultar horarios semanais e aulas geradas por periodo. Endpoints dedicados filtram agenda e horarios por professor dentro da organizacao. E2E com PostgreSQL/API/Admin validou associacao e consulta da aula; testes de integracao tambem passaram localmente em 2026-10-31.
- Em 2026-11-01, a graduacao passou a organizar faixas separadamente dos graus: cada faixa define sua quantidade de graus, e cada avanço tem critérios/aulas configuráveis. A migration `008_graduation_belts_and_degrees.sql` converte níveis legados em faixas sem perder critérios. A API e o Admin permitem associar graduação somente a modalidades do tipo `martial_art` (modalidades ativas para novos cadastros); testes PostgreSQL cobrem sequência, limite de graus, conversão legada e isolamento por organização.
- A migration `009_remove_non_martial_graduation_data.sql` remove sistemas, faixas, níveis e históricos de graduação ligados a modalidades que não são artes marciais, inclusive históricos que apontem para níveis inválidos. Os testes PostgreSQL confirmam que a limpeza não apaga sistemas e níveis marciais válidos.
- A configuração de graduação permite editar faixas, critérios e graus. Exclusões removem apenas itens sem histórico; graus são excluídos do fim da sequência para manter a ordem, e quantidades de graus não podem ficar abaixo dos já cadastrados.
- Em 2026-11-01, a navegação do Admin foi agrupada em áreas compartilhadas, Artes Marciais e Fitness/Condicionamento. `GET /api/v1/workspace/capabilities` retorna flags tenant-scoped derivadas apenas das modalidades ativas; `hybrid` habilita as duas áreas, mas graduação continua exclusiva de `martial_art`. O dashboard também oculta a aba/consulta de graduações quando não há modalidade marcial ativa. Passaram 54 testes unitários do backend, 25 do Admin e 1 teste PostgreSQL dedicado à consulta dos tipos ativos.
- Em 2026-11-02, as seções Principal, Gestão da academia, Artes marciais, Rotina, Comunidade e Administração passaram a ser expansíveis/recolhíveis de forma independente; Musculação e Fitness também tem seção própria quando aplicável. Os controles expõem o estado em `aria-expanded`; 26 testes unitários do Admin passaram, incluindo o recolhimento independente das seis seções nomeadas.
- Em 2026-11-02, um E2E integrado passou pelas jornadas de graduação (faixa, grau, promoção), Fitness (meta, progresso e histórico), ranking (criação/recalculo de ciclo e concessão de medalha), comunicado e auditoria. As cinco telas não apresentaram overflow horizontal em viewport de 390 px. O fluxo encontrou e permitiu corrigir a gravação de progresso fitness: `person_id` era tratado incorretamente como `teacher_id`, causando erro de chave estrangeira para administradores. A resolução agora associa o autor a um professor apenas quando existe vínculo na mesma organização; E2E e teste de integração PostgreSQL validam os casos de administrador/aluno e professor.

## Entrega do painel Admin — 2026-11-11

Esta atualização prevalece sobre descrições históricas conflitantes abaixo. A entrega trata das jornadas do painel; não representa conclusão das pendências de produção/infraestrutura.

- [x] Cadastro de pessoas/equipe independente de aluno/professor; o diretório e os dois formulários operacionais permitem acrescentar vínculos à mesma pessoa, preservando identidade e dados existentes.
- [x] Gestão de contas existentes: e-mail de login, múltiplos papéis, bloqueio/reativação, redefinição temporária de senha e encerramento de sessões. A própria conta e contas superiores são protegidas nas operações administrativas.
- [x] Proteção central de rotas/menu e destino após login por perfil. Consulta `.read` foi separada de gestão `.manage` nos quatro cadastros; permissões incompatíveis com o papel são rejeitadas.
- [x] Área web do aluno: agenda/check-in, presença, graduação, metas e registros, conquistas, faturas e pagamentos pessoais. O servidor resolve a identidade pelas claims e rejeita tentativas de acesso aos dados de outro aluno.
- [x] Área do professor e escopo de suas aulas/chamadas/check-ins pendentes nas rotas web e mobile.
- [x] Jornada administrativa do aluno reúne cadastro, modalidades, financeiro, presença, graduação, metas e conquistas, reutilizando os componentes pessoais quando adequado.
- [x] Paginação/busca no servidor nos cadastros e aulas; paginação nas jornadas financeiras, notificações e rankings; listas de opções completas. O perfil financeiro usa opções do próprio contexto, sem exigir acesso ao cadastro administrativo de alunos.
- [x] Entradas Gym/Fight com cores, ícones e atalhos distintos, navegação conforme modalidades ativas, contexto compartilhado explícito e layout adaptado ao celular. Metas físicas continuam disponíveis para alunos de artes marciais; graduação é exclusiva de modalidades marciais.
- [x] Dados/preferências da academia, check-in habilitável e confirmação opcional; troca voluntária e recuperação de senha. O transporte SMTP está implementado e requer credenciais externas para entrega real.
- [x] Metas de aumento/redução com valor inicial e cálculo consistente no Admin e nos modelos do aplicativo.
- [x] Migrations `011`–`013` aplicadas em transação e registradas no PostgreSQL local `dev`, sem excluir registros. Schema consolidado, seed de produção e DBML atualizados.

Validação executada: **59 testes unitários backend, 13 de integração (incluindo PostgreSQL real e bootstrap consolidado idempotente), 1 arquitetural, 31 unitários/componentes Admin, 9 E2E Chromium/API/Admin/PostgreSQL e 9 testes Flutter: 122 aprovados, zero falhas**. `flutter analyze --no-pub` não apontou problemas. `git diff --check` aprovado. E2E verificou Gym/Fight e o portal em 390 px e gerou capturas temporárias. A suíte cobre também vínculo de uma pessoa existente pelos dois formulários, perfil financeiro, consulta sem gravação, bloqueio de conta, recuperação de senha com revogação e isolamento do professor/aluno.

Não foi feita publicação/deploy nem configuração de credenciais SMTP. Nenhum commit foi criado.

### Visões Geral, Fight e Gym — 2026-11-11

- [x] Seletor reutilizável nas listas e metas; atalhos e menu preservam a área na URL após navegação/reload. Owner, admin e recepção acessam suas visões sem receber permissões adicionais.
- [x] Filtros de alunos, professores, modalidades, aulas e metas no servidor antes de paginação/contagem. Vínculos com modalidades determinam as áreas; híbridas entram nas duas, múltiplos vínculos não duplicam alunos e a Geral inclui quem ainda não tem modalidade. Cadastro/jornada únicos, financeiro e dashboard gerais.
- [x] Opções de metas e graduação coerentes com a área; metas vinculadas a modalidade seguem sua área e metas pessoais sem modalidade seguem os vínculos do aluno. Nenhuma migration ou alteração de dados foi necessária.

Validação desta atualização: **59 testes unitários backend, 18 de integração PostgreSQL, 1 arquitetural, 34 unitários/componentes Admin e 13 E2E: 125 aprovados**. Os três perfis foram exercitados no navegador com troca de área e reload; a integração cobre busca/paginação acima de 25 alunos, tipos híbridos, vínculos cruzados de tenant, escopo do professor e metas com/sem modalidade. Capturas mobile/desktop foram inspecionadas; `git diff --check` aprovado. Flutter não foi alterado nesta atualização.

### Revisão das queries — 2026-11-11

- [x] Queries da entrega revisadas e indentadas no padrão de `BranchQueries`. As classes de queries agora expõem 170 constantes SQL completas; removidos os construtores de SQL, concatenações de fragmentos, interpolação, `Replace` e `Split`.
- [x] Quatro consultas auxiliares foram movidas dos repositórios para constantes de seus contextos. Valores continuam separados do SQL em parâmetros nomeados do Dapper, incluindo busca, área, período, paginação e tenant.
- [x] Comparação do SQL compilado antes/depois confirmou equivalência das 166 consultas existentes, ignorando apenas espaços e quebras de linha. Nenhuma migration ou alteração de dados foi necessária.
- [x] Convenção protegida por teste arquitetural; cinco casos de integração verificam aspas, curingas/barras e tentativas de SQL injection nas buscas de alunos, professores, unidades, modalidades e pessoas, incluindo contagens e isolamento por academia.

Validação: **59 unitários backend, 23 integração PostgreSQL, 2 arquiteturais, 34 unitários/componentes Admin e 13 E2E: 131 aprovados**. O teste de graduação confirma o campo por saída de foco antes do envio, e as 13 jornadas passaram juntas na execução final. Build, comparação do SQL e `git diff --check` aprovados. API local reiniciada e API/Admin respondendo HTTP 200. Nenhum commit foi realizado.

### Política de senha — 2026-11-11

- [x] Novas senhas exigem no mínimo 6 caracteres, uma letra maiúscula, uma minúscula e um caractere especial. Regra centralizada em `Contracts/Auth/PasswordPolicy.cs` e usada nos cadastros, bootstrap da plataforma, senhas temporárias, troca e recuperação.
- [x] Painel usa a mesma política e mensagem, com validação visível no primeiro acesso. O aplicativo Flutter aplica uma política equivalente, incluindo letras/símbolos Unicode e contagem de caracteres.
- [x] Jornadas reais usam senhas de seis caracteres e verificam rejeição de regras incompletas, primeiro acesso, recuperação, revogação de sessões e troca em Minha conta. Não houve migration nem alteração das senhas existentes na base.

Validação: **81 unitários backend, 23 integração PostgreSQL, 2 arquiteturais, 34 unitários/componentes Admin, 13 E2E e 26 testes Flutter: 179 aprovados**. Build sem avisos/erros; `flutter analyze --no-pub` e `git diff --check` aprovados. Os processos descartáveis dos testes foram encerrados; nenhuma API/Admin de desenvolvimento ficou executando em segundo plano. Nenhum commit foi realizado.

### Contagem de linhas — 2026-11-11

- [x] Padronizadas 31 ocorrências de contagem de linhas para `count(1)`: 20 nas queries da aplicação, 9 nas verificações de testes e 2 no bootstrap do banco. Preservados `FILTER`, janelas, casts, contagens por coluna e `DISTINCT`.
- [x] Queries continuam constantes e parametrizadas. A mudança é uma padronização; não foi atribuído ganho de desempenho sem medição no PostgreSQL.

Validação: **81 unitários backend, 23 integração PostgreSQL, 2 arquiteturais e 13 E2E: 119 aprovados**. Sintaxe do bootstrap verificada com `bash -n`; `git diff --check` aprovado. Nenhuma migration, alteração de dados ou commit foi realizado.

## Inventario Verificado

| Area | Estado atual | Evidencia e limite |
|---|---|---|
| API e arquitetura | Presente, validacao operacional incompleta | Projetos Api, Core, Contracts, UseCases, Repositories e Services; controllers para autenticacao, organizacoes, unidades, clientes, professores, modalidades, aulas, presenca, financeiro, fitness, graduacao, ranking, notificacoes, relatorios, auditoria e controle de acesso. Rotas de professor agora retornam horarios associados e aulas filtradas por professor/periodo; teste de integracao confirma query em PostgreSQL. Outras rotas ainda precisam de validacao operacional. |
| Autenticacao | Sessões, troca e recuperação implementadas | Versão da sessão e estado atual da conta/permissões são verificados na API; bloqueio, alteração de login/senha e encerramento administrativo invalidam acessos anteriores. Recuperação web e serviço agnóstico de e-mail implementados; SMTP validado em captura local real, incluindo consumo do link e revogação. Entrega por fornecedor externo ainda exige configuração e validação. |
| Tenant e autorizacao | Matriz aplicada e jornadas principais verificadas | Papéis/permissões efetivos continuam tenant-scoped; quatro permissões de consulta foram separadas de gestão. Menu, rotas e API respeitam a matriz. E2E valida consulta sem gravação, aluno restrito aos próprios dados, professor restrito às próprias aulas, perfil financeiro e proteção da própria conta. O MVP mantém uma organização por conta. |
| Auditoria | Transacional na plataforma; parcial nas demais áreas | O módulo da plataforma persiste mutações e auditoria na mesma transação, com consulta global e registro dos acessos. Middleware e endpoint de consulta local existem. A gravacao ocorre depois da resposta da mutacao e falhas sao capturadas; assim, uma operacao pode concluir sem auditoria persistida. Integridade/atomicidade nao foi comprovada. |
| Banco de dados | Migrations e schema consolidado validados | Migrations `001`–`016`; `011`–`016` aplicadas em transação no banco local `dev`, com ledger, sem exclusão de registros. A `014` cria configurações ausentes de academias legadas, sem alterar políticas existentes. Integração cria banco descartável com o schema de produção e reaplica schema/seed duas vezes; 39 tabelas correspondem ao DBML, incluindo `platform_incidents` e métricas agregadas. Validação de rollback/recuperação operacional continua na fase de produção. |
| Testes do backend | 106 testes aprovados | 81 unitários, 23 integração e 2 arquiteturais aprovados localmente em 2026-11-11. Cobre política de senha, SQL constante, buscas parametrizadas com tentativas de injection, visões por área, configurações de academias legadas, filtros de aulas com offsets -03:00/+09:00, identidade compartilhada, mudança/bloqueio de conta, catálogo além dos primeiros 100, metas de redução, políticas de check-in e schema consolidado idempotente. |
| Admin | Jornadas de gestão e portal pessoal entregues | Diretório/equipe e contas existentes, vínculo de pessoas pelos formulários, consulta sem gravação, paginação/busca, configurações, recuperação de senha, áreas pessoais de aluno/professor e entradas Gym/Fight. A jornada do aluno reutiliza componentes de presença, graduação, metas e conquistas. Componentes novos separam apresentação e lógica. |
| Testes do Admin | 41 testes unitários/componentes e 19 E2E aprovados | Chromium/API/Admin/PostgreSQL reais: três visões de alunos para owner/admin/recepção, bootstrap, operação/financeiro, navegação, graduação/fitness/comunidade/auditoria, equipe/bloqueio/consulta, identidade nos dois formulários, portal privado e check-in, escopo do professor/perfil financeiro e recuperação com revogação. Aulas verificadas em America/Sao_Paulo; Gym/Fight e portal em 390 px. |
| Mobile | Presente, escopo inicial | `frontend/kiai-control-mobile`: login/sessão, aulas/check-in, chamada do professor, perfil, metas e troca de senha com a mesma política da API. 26 testes (9 modelos, 17 senha) e análise estática aprovados nesta entrega. Widget/integration tests e ampliação das jornadas mobile continuam no backlog. |
| Infraestrutura e CI/CD | CI parcial validado em runner limpo; nova etapa E2E aguarda execucao remota | Workflow GitHub Actions compila API/Admin, testa API/Admin e publica TRX; agora tambem instala Chromium e exige TRX do E2E. A execucao anterior `41257017649` passou o workflow entao existente e publicou `test-results` (`13063921778`); ela nao inclui as mudancas Playwright atuais. Nao foram encontrados Dockerfiles, Compose, deploy manifests ou configuracao de observabilidade. |
| Health e observabilidade | Parcial | Correlation ID, logging de falha de auditoria e painel global de ocorrências HTTP 5xx, login 401 e falhas de auditoria existem. Há métricas agregadas por academia, gráfico diário e readiness com probes de PostgreSQL/cache. E2E confirma readiness 503 e liveness 200 com PostgreSQL indisponível. Redis real ainda precisa de validação operacional. Nao foi encontrada integracao OpenTelemetry/Prometheus/Grafana. |

## Alinhamento dos Documentos

- `SYSTEM_DESIGN.md` permanece como referencia de arquitetura e escopo desejado; o mobile está iniciado; observabilidade e deploy continuam pendentes.
- `CHECKPOINT.md` e um snapshot de 2026-10-08. A afirmacao de 31 testes aprovados nao substitui a execucao atual; os projetos de teste contem placeholders.
- `API_IMPLEMENTATION_PLAN.md` e de 2026-10-04 e esta desatualizado: controle de acesso, auditoria, fitness, graduacao, ranking, notificacoes e relatorios ja possuem controllers/camadas, e existem migrations posteriores a `001`.
- O README da API dizia que os endpoints eram apenas smoke endpoints; isso nao representa mais a estrutura atual. O README do Admin sugeria `dotnet test` apesar de nao existir projeto de teste.
- `COPILOT.md` e `.agent.md` devem direcionar os agentes para este plano antes de evoluir o produto.

## Plano de Evolucao

### Fase 0 - Linha de Base Confiavel (P0)

- [x] Revalidar a solution completa da API com Docker depois da migration `005`; em 2026-10-31 passaram 50 testes unitarios, 1 arquitetural e 4 de integracao PostgreSQL. Foi corrigido o teste arquitetural para normalizar separadores Windows/Unix nos `ProjectReference`.
- [x] Validar a migration `006` e o fluxo aberto por modalidade na solution completa; passaram 54 testes unitarios, 1 arquitetural e 5 de integracao PostgreSQL. O Admin passou no build e em 15 testes unitarios.
- [x] Remover o modelo de turmas/matriculas fixas via migration `007`, converter aulas/ciclos antigos para modalidade e validar ausencia das tabelas/colunas no schema final.
- [x] Gerar e validar localmente um TRX independente por projeto ativo: API em `backend/kiai-control-api/TestResults/{unit,integration,architecture}` e Admin em `frontend/kiai-control-admin/tests/TestResults/{admin,e2e}`, junto da estrutura de testes.
- [x] Confirmar que a pipeline publica os TRX como artifact apos sua primeira execucao no GitHub Actions; a execucao `41162725604` publicou `test-results` (artifact `13027836463`), embora o job tenha falhado no teste arquitetural.
- [x] Substituir os smoke tests `Assert.True(true)` por verificacoes reais ou removê-los ate existir comportamento testavel. O teste de arquitetura compara `ProjectReference` com o grafo permitido; o teste de bootstrap resolve repositorios registrados no DI; placeholders sem comportamento foram removidos.
- [x] Criar workflow de CI para restore/build da API e Admin, testes por projeto e falha quando TRX esperado estiver ausente, vazio ou com falhas. Comandos e validacao dos quatro TRX passaram localmente; o GitHub Actions ja publicou um artifact na execucao `41162725604`, que falhou depois no teste arquitetural.
- [x] Confirmar uma execucao bem-sucedida do workflow em runner limpo e a publicacao dos TRX de todos os projetos; GitHub Actions run `41257017649` passou os builds, 48 testes unitarios, 6 de integracao PostgreSQL, 1 arquitetural, 15 do Admin, verificacao de TRX e publicou o artifact `test-results` (`13063921778`).
- [x] Criar projeto de testes do Admin e cobrir refresh, tenant, nome obrigatorio, erro HTTP, navegacao por permissions e bloqueio de `/access` sem permission; 15 testes passaram localmente em 2026-10-31, incluindo persistencia/restauracao, refresh e estados da tela de acesso.
- [x] Cobrir a tela de gestao de permissions e os estados de erro/carregamento; em 2026-10-31 passaram 4 testes de componente, incluindo usuario autorizado, carregamento, erro HTTP e bloqueio sem permission.
- [x] Cobrir localmente fluxos E2E criticos de login, criar/editar aluno, vincular modalidade, registrar presenca, validar check-in e registrar pagamento; 1 cenário Playwright passou contra Chromium, PostgreSQL 17, API e Admin em 2026-10-31. O E2E também revelou e permitiu corrigir a rota `Location` do endpoint de geração de aulas.
- [ ] Executar a nova etapa Playwright no GitHub Actions, validar o TRX E2E e confirmar que o artifact contém o relatório.
- [x] Atualizar a matriz de estado deste documento quando uma validacao mudar o status de uma area; evidencia local e resultado do runner registrados em 2026-10-31.

**Aceite:** pipeline reproduzivel em ambiente limpo, relatorios TRX preservados por projeto, contagem de testes visivel, nenhum placeholder contado como cobertura e documentos sem afirmacoes contraditas pelo codigo.

### Fase 1 - PostgreSQL, Migrations e Integracao (P0)

- [x] Implementar fixture Testcontainers com PostgreSQL 17 que aplica migrations `001`-`009` em ordem e descarte a instancia ao final; testes de integracao PostgreSQL validaram o schema, a conversao de configuracoes antigas de graduacao e a remocao de registros invalidos de modalidades nao marciais.
- [x] Executar a fixture em runner com Docker e validar a conexao real, migrations e round-trip de `DateOnly`; o passo de integracao PostgreSQL passou no GitHub Actions run `41162725604` e os 4 testes passaram localmente em 2026-10-31.
- [x] Documentar o comando de execucao em ambiente com Docker e configurar o runner de CI para executar o teste de integracao; comando em `backend/kiai-control-api/README.md` e passo de integracao em `.github/workflows/backend-api-ci.yml`.
- [x] Aplicar todas as migrations e o seed em banco vazio; validar schema, bootstrap e login administrativo. O E2E existente confere migrations e bootstrap com PostgreSQL 17 descartavel; o teste de navegador nao foi repetido nesta alteracao.
- [ ] Completar validacao do controle de versao/aplicacao de migrations: o E2E confirma ledger e idempotencia ao reaplicar o bootstrap; o script tambem foi executado no banco local `dev`. Falta validar schema inicial existente sem ledger. Rollback documentado continua pendente.
- [ ] Adicionar testes de integracao para os repositorios Dapper: CRUD, constraints, datas `DateOnly`, transacoes e isolamento entre organizacoes.
- [ ] Validar os fluxos completos de criacao/edicao de cliente e professor, vinculos de modalidade, chamadas, pagamentos e consultas diretamente contra PostgreSQL.
- [ ] Provar idempotencia da geracao de aulas/faturas com repeticao da mesma solicitacao.
- [ ] Criar fixtures com duas organizacoes e comprovar que nenhuma consulta ou mutacao cruza o tenant.

**Aceite:** banco vazio pode ser criado e migrado de forma documentada; migrations/seed executam na ordem; testes reais de repositorio e tenant passam; build/unit tests nao sao usados como substitutos da integracao.

### Fase 2 - Seguranca, Tenant, Permissoes e Auditoria (P0)

- [ ] Decidir e documentar o modelo de organizacao para uma conta: manter uma organizacao no MVP ou suportar multiplas. Se suportar multiplas, ajustar schema, claims, selecao de contexto, refresh e consultas; nunca confiar isoladamente no header do cliente.
- [x] Conectar permissoes efetivas carregadas do banco a claims JWT e policies por modulo; controllers exigem policy e preservam as roles existentes. Teste sem banco prova negar claim ausente e aceitar claim correspondente.
- [x] Separar defaults de permissions de overrides por organizacao em `organization_role_permissions`; mutacoes levam tenant/ator e permissions do role `owner` sao imutaveis pela API.
- [ ] Testar cada papel contra os endpoints em PostgreSQL/host de teste, incluindo migrations `004`/`005` e revogacao apos refresh/expiracao do access token.
- [ ] Impedir escalacao de role/permissao e testar concessao/revogacao entre organizacoes, inclusive tentativas de `owner`.
- [x] Parar de retornar `ResetToken` na API e manter resposta indistinguivel para e-mail existente/inexistente.
- [x] Implementar entrega do reset pelo serviço agnóstico de e-mail e validar recebimento/consumo em captura SMTP local; limitação por IP existente preservada.
- [ ] Configurar provedor/remetente externo e validar entrega real; complementar a limitação de tentativas por conta.
- [x] Limitar login, refresh, logout, change-password e reset a 10 requisicoes por IP/minuto.
- [ ] Configurar limites por ambiente e aplicar rate limiting aos demais endpoints publicos.
- [x] Remover a chave JWT fallback do appsettings geral; validar issuer/audience, lifetimes e chave HS256 >= 32 bytes; usar fallback apenas em Development e falhar startup em outros ambientes quando a chave estiver ausente.
- [ ] Auditar mutacoes criticas com garantia de persistencia coerente com a operacao (transacao ou outbox); cobrir falha de auditoria e isolamento por tenant.
- [ ] Revisar logs e artefatos de excecao para remover `Authorization`, JWTs, refresh/reset tokens e dados pessoais. Um artefato de excecao compartilhado durante a analise continha header `Authorization`; remover/guardar o artefato e revogar o token se ainda puder ser valido.
- [ ] Completar testes 401/403, tenant cruzado, enumeracao de contas, rotacao/reuso de refresh e reset de senha.

**Aceite:** autorizacao efetiva e testada no servidor; segredo ausente impede configuracao insegura; recuperacao nao vaza token; logs nao armazenam credenciais; auditoria critica e duravel.

### Fase 3 - Operacao e Prontidao para Producao (P0 antes do deploy)

- [x] Registrar probes de PostgreSQL e cache configurado; E2E com PostgreSQL real confirma `/health/ready` 503 e `/health/live` 200 quando o banco para.
- [ ] Validar Redis real saudável/indisponível e repetir os checks em staging.
- [ ] Padronizar tratamento de excecoes/Problem Details em todos os ambientes e revisar mensagens que possam revelar detalhes internos.
- [ ] Implementar OpenTelemetry para traces, metricas e logs; definir correlation/trace IDs e dashboards/alertas operacionais.
- [ ] Criar Dockerfiles/Compose para desenvolvimento e pipeline de build/publicacao/deploy; documentar variaveis sem valores secretos.
- [ ] Automatizar backup e testar restauracao do PostgreSQL; documentar rollback de deploy e migrations.
- [ ] Fazer deploy de staging e smoke tests autenticados da API e do Admin antes de declarar release.

**Aceite:** readiness representa dependencias reais; backup foi restaurado em teste; deploy/rollback e observabilidade sao demonstrados em staging.

### Fase 4 - Fechar os Fluxos do Painel Admin (P1)

- [x] Unificar a jornada do aluno entre cadastro/modalidades e assinatura financeira; a acao Jornada do aluno em `/clients` consolida cadastro, modalidades, assinaturas, faturas e pagamentos, com atalhos para editar cadastro, criar/encerrar assinatura e registrar pagamento. Consultas financeiras filtram no servidor por aluno/organizacao; no E2E o fluxo real passou em PostgreSQL/API/Admin.
- [x] Completar a jornada do professor: a acao Agenda e horarios em `/teachers` associa/desassocia o professor em horarios recorrentes por modalidade/unidade e consulta aulas geradas em intervalo de datas. Rotas API de horarios/aulas sao tenant-scoped e filtram no servidor por professor; integracao PostgreSQL e E2E passaram localmente em 2026-10-31.
- [x] Implementar telas para planos, assinaturas, faturas e pagamentos, com confirmacao de cancelamento/desativacao e validacao de saldo/pagamento; build Admin aprovado, aguardando validacao com API/PostgreSQL real.
- [x] Implementar a jornada de graduacao: `/graduation` configura sistemas apenas para modalidades de arte marcial, registra faixas em ordem, define quantos graus cada uma possui e configura aulas/criterios por avanço. A promoção segue sujeita a confirmação manual e conta aulas desde a última graduação. Em 2026-11-01 passaram os testes unitarios/integração, build Admin e testes unitarios Admin; o E2E nao foi executado nesta alteracao.
- [x] Implementar metas e progresso fitness: `/fitness` lista e filtra objetivos por aluno, cria metas vinculadas a modalidade opcional, registra progresso, mostra percentual/estado e permite cancelar metas ativas; rotas de opcoes sao protegidas por `fitness.manage`. Build API/Admin, 20 testes unitarios Admin e teste PostgreSQL de progresso/conclusao/cancelamento passaram em 2026-10-31.
- [x] Consultar o historico paginado de progresso das metas: endpoint `GET /fitness-goals/{goalId}/progress` restringe a consulta a organizacao autenticada; a tela `/fitness` exibe data, valor e observacoes, inclusive para metas concluidas/canceladas. Teste PostgreSQL validou isolamento por organizacao e paginacao; teste Admin validou rota e parametros em 2026-10-31.
- [x] Implementar rankings e medalhas: `/rankings` mostra classificacao atual e ciclos, permite criar/recalcular ciclos conforme role, cadastrar medalhas e conceder/consultar medalhas por aluno; opcoes sao tenant-scoped sob `ranking.manage`. Build API/Admin, 21 testes unitarios Admin e teste PostgreSQL de recálculo e concessao de medalha passaram em 2026-10-31.
- [x] Implementar notificacoes: `/notifications` oferece caixa pessoal e leitura; perfis com `notifications.manage` tambem consultam o outbox e registram comunicados internos a alunos. O Admin nao promete entrega por e-mail/push sem provedor. Build API/Admin, 22 testes unitarios Admin e teste PostgreSQL de outbox, inbox, leitura e isolamento passaram em 2026-10-31.
- [x] Implementar auditoria: `/audit` lista eventos com filtros de periodo, paginacao e detalhes tecnicos; acesso visual/API exige `audit.read` e role owner/admin. IDs internos sao ocultados/sanitizados na tela. Build API/Admin, 23 testes unitarios Admin e teste PostgreSQL de consulta temporal/isolamento passaram em 2026-10-31.
- [x] Integrar os relatorios existentes ao dashboard e permitir filtros previstos pelo contrato: `/` exibe os relatorios de dashboard, frequencia, financeiro, desempenho de alunos e graduacoes; filtros de data e modalidade, alem da paginacao do desempenho, sao enviados aos endpoints existentes. `/reports/modalities` fornece opcoes de modalidade sob a permission `reports.read`. Em 2026-10-31 passaram build da API/Admin, 17 testes unitarios Admin (incluindo os parametros das chamadas) e teste de integracao PostgreSQL validando os relatorios de frequencia, desempenho, dashboard, financeiro e graduacoes.
- [x] Organizar a navegacao por areas sem duplicar recursos compartilhados: endpoint autenticado e tenant-scoped retorna capacidades agregadas das modalidades ativas; o menu respeita permissions e separa Artes Marciais e Fitness/Condicionamento. `hybrid` habilita as duas areas, mas nao graduacao sem modalidade `martial_art`; o dashboard oculta sua aba e consulta de graduacoes quando nao aplicavel. Em 2026-11-01 passaram 54 testes unitarios backend, 25 testes unitarios Admin e teste PostgreSQL da consulta de tipos ativos. Em 2026-11-02 passaram 26 testes Admin e os 4 cenarios E2E com PostgreSQL/API/Admin reais, incluindo navegacao independente, jornadas de negocio e checagem de overflow mobile. Revisao ampla de roles, estados e responsividade de todos os fluxos continua pendente.
- [ ] Revisar navegacao por role/permission, estados vazios, carregamento, erros, paginacao e responsividade em todos os fluxos.
- [x] Validar os fluxos E2E criticos existentes: login, criar/editar aluno, vincular modalidade, registrar presenca, validar check-in e registrar pagamento. Os 2 cenarios Playwright passaram na validacao local completa.
- [x] Adicionar E2E de navegador para as jornadas de graduacao, fitness, rankings, notificacoes e auditoria. Em 2026-11-02, o teste percorreu graduacao inicial e promocao, meta e historico de progresso, ciclo e recalculo de ranking, concessao de medalha, comunicado e consulta de auditoria; tambem verificou overflow horizontal das cinco telas em viewport de 390 px. Os 4 cenarios E2E passaram localmente com PostgreSQL/API/Admin reais.
- [ ] Completar testes de componente para estados de carregamento, erro, vazio e permissoes das novas jornadas; revisar visualmente todos os fluxos, nao apenas overflow em viewport movel.

**Aceite:** cada fluxo operacional do MVP pode ser concluido pelo Admin sem chamadas manuais/API externa; autorizacao visual acompanha a autorizacao do backend; E2E dos fluxos criticos passa.

### Fase 5 - Regras de Dominio e API (P1)

- [ ] Comparar cada modulo ja existente com os requisitos RF/UC de `SYSTEM_DESIGN.md`; registrar lacunas sem recriar controllers/CRUDs existentes.
- [ ] Formalizar por decisao tecnica as regras ainda ambiguas de graduacao, elegibilidade, pontos, streak, desempate, ciclos de ranking, categorias fitness e excecoes administrativas.
- [ ] Cobrir regras de dominio com unit tests e queries/constraints com integracao PostgreSQL, incluindo concorrencia e repeticao de comandos.
- [ ] Conferir contrato/OpenAPI e erros HTTP para cada fluxo; garantir paginacao e limites defensivos nas listagens/relatorios.
- [ ] Validar privacidade `HideFromRanking`, estados de aluno/professor e restricoes por papel em todas as respostas/relatorios.

**Aceite:** requisitos aplicaveis tem regra explicita, contrato, validacao e testes; nao ha comportamento de negocio inferido somente da interface ou de documentacao antiga.

### Fase 6 - Aplicativo Mobile (P2, escopo de produto)

- [x] Projeto Flutter presente em `frontend/kiai-control-mobile`; estruturas Android/iOS e nove testes unitários de modelos verificados em 2026-11-11.
- [ ] Definir contratos e fluxo de organizacao/sessao para pessoas com os papeis de aluno e professor.
- [ ] Entregar primeiro login/sessao, agenda/check-in do professor e consulta do aluno (presenca, evolucao, graduacao e pagamentos).
- [ ] Adicionar testes unitarios, widget e integration_test para os fluxos mobile criticos.

**Aceite:** app consome API versionada sem compartilhar implementacoes internas do backend e respeita tenant, papeis e ciclo de vida de tokens.

## Definition of Done por Entrega

- [ ] Requisito e comportamento esperado foram identificados em `SYSTEM_DESIGN.md` ou numa decisao aprovada.
- [ ] Mudanca foi feita na camada proprietaria, sem duplicar API, query ou componente existente.
- [ ] Validacoes de entrada, erros HTTP, autorizacao, tenant e dados sensiveis foram considerados.
- [ ] Teste adequado ao risco foi adicionado e executado; SQL foi validado contra PostgreSQL real quando aplicavel.
- [ ] Build/testes da area passaram e a saida de execucao foi observada, nao presumida.
- [ ] README, contratos, migration e este plano foram atualizados quando aplicavel.
- [ ] Nenhum token, segredo ou dado pessoal foi incluido em logs, screenshots, fixtures ou documentos.

## Fontes e Limites da Analise

Inventário histórico iniciado em 2026-10-30, atualizado pela entrega e validação local de 2026-11-11 descrita acima. PostgreSQL real, API/Admin e navegador foram executados; deploy, restauração de backup, execução remota da pipeline atualizada e entrega SMTP externa não foram verificados nesta sessão.
