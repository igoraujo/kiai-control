# SYSTEM_DESIGN.md

# Kiai Control — System Design

**Versão:** 1.1  
**Data:** 2026-07-03    
**Stack principal:** .NET 10, Blazor Server, Flutter, PostgreSQL, Redis, OpenTelemetry, Prometheus, Grafana    
**Modelo arquitetural:** SaaS modular, arquitetura em camadas pragmática baseada em API, Core, Contracts, UseCases, Repositories e Services, utilizando DDD tático, CQRS simplificado e Repository Pattern.    
**Decisão desta revisão:** monorepo temporário para estruturação com IA, porém cada projeto nasce preparado para virar repositório próprio.    

---

## 1. Visão Geral

### Nome do sistema

**Kiai Control**

### Objetivo

Plataforma SaaS com CRM web, API e aplicativo mobile para gestão de academias de artes marciais e atividades físicas, com controle de alunos, professores, modalidades, turmas, horários, presença, mensalidades, pagamentos, graduação, evolução, metas, ranking e relatórios.

### Problema resolvido

Centraliza controles antes feitos em planilhas e ferramentas desconectadas, reduzindo inadimplência, perda de histórico, falhas de presença, baixa visibilidade de desempenho dos alunos e dificuldade de gestão operacional.

Academias pequenas e médias normalmente controlam presença, pagamentos, evolução, graduação e comunicação com alunos em planilhas, mensagens avulsas ou sistemas genéricos. Isso gera perda de informação, inadimplência, falta de previsibilidade financeira, baixa retenção de alunos e dificuldade para acompanhar desempenho individual.

O Kiai Control centraliza esses controles em uma única plataforma, com foco em:

- Artes marciais com regras de graduação por modalidade.
- Atividades físicas com metas, progresso e categorias de evolução.
- Controle financeiro recorrente.
- Registro de presença por aula.
- Perfis múltiplos para uma mesma pessoa, por exemplo aluno e professor.
- Múltiplas modalidades por aluno.
- Experiência web para gestão e mobile para alunos/professores.

### Benefícios

- Redução de inadimplência por controle de mensalidades, vencimentos e alertas.
- Gestão centralizada de alunos, professores, turmas e pagamentos.
- Controle de frequência e presença por aula.
- Evolução marcial com faixas, graus e critérios de graduação.
- Evolução física com metas, progresso, categorias e pontuação.
- Gamificação com ranking, medalhas e dias sem faltar.
- API única para painel web e app mobile.
- Base arquitetural preparada para crescimento e separação futura em repositórios independentes.
- Menor dependência de planilhas.
- Acesso responsivo por web e aplicativo mobile.

### Escopo

Inclui:

- API REST .NET 10.
- Painel administrativo Blazor Server + MudBlazor.
- App mobile Flutter.
- PostgreSQL, Redis, OpenTelemetry, Prometheus e Grafana.
- Clean Architecture, DDD, CQRS e Repository Pattern.
- Monorepo inicial com projetos desacoplados e READMEs individuais.
O sistema contempla:

- Cadastro e gestão de organizações/academias.
- Gestão de usuários, pessoas e múltiplos perfis.
- Cadastro de alunos, professores e administradores.
- Gestão de modalidades.
- Gestão de turmas, horários e aulas.
- Registro de presença.
- Controle de mensalidades, planos e pagamentos.
- Relatórios financeiros, operacionais e de desempenho.
- Graduação por faixa/grau para artes marciais.
- Metas, progresso, pontuação e categorias para atividades físicas.
- Ranking de frequência e medalhas.
- Autenticação JWT com refresh token.
- Autorização RBAC por organização e perfil.
- API REST para web e mobile.
- Observabilidade com OpenTelemetry, Prometheus e Grafana.
- Deploy inicial em Hostinger VPS via Coolify e Docker.

### Limites do sistema

Fora do MVP:

- Marketplace público.
- Emissão fiscal.
- Processamento financeiro próprio.
- Integração nativa com catracas/biometria.
- IA avançada.
- Streaming de vídeo-aulas.
- WhatsApp oficial via API paga.
- Kubernetes em produção inicial.

---

## 2. Requisitos Funcionais

### Gestão de conta, organização e usuários

- **RF001** - O sistema deve permitir cadastro de organização/academia.
- **RF002** - O sistema deve permitir edição dos dados da organização.
- **RF003** - O sistema deve permitir cadastro de unidades/filiais da organização.
- **RF004** - O sistema deve permitir cadastro de pessoas.
- **RF005** - O sistema deve permitir que uma pessoa tenha múltiplos perfis: aluno, professor, administrador, financeiro e recepção.
- **RF006** - O sistema deve permitir ativar e desativar usuários.
- **RF007** - O sistema deve permitir autenticação por e-mail e senha.
- **RF008** - O sistema deve permitir recuperação de senha.
- **RF009** - O sistema deve permitir troca de senha autenticada.
- **RF010** - O sistema deve permitir emissão de token JWT e refresh token.
- **RF011** - O sistema deve permitir encerramento de sessão.
- **RF012** - O sistema deve permitir revogação de refresh tokens.
- **RF013** - O sistema deve permitir controle de acesso por papéis e permissões.

### Gestão de modalidades

- **RF014** - O sistema deve permitir cadastro de modalidades.
- **RF015** - O sistema deve permitir classificar modalidade como arte marcial, atividade física ou híbrida.
- **RF016** - O sistema deve permitir que uma pessoa pratique múltiplas modalidades.
- **RF017** - O sistema deve permitir vincular professores a modalidades.
- **RF018** - O sistema deve permitir definir regras de graduação por modalidade marcial.
- **RF019** - O sistema deve permitir definir metas padrão por modalidade física.

### Gestão de alunos

- **RF020** - O sistema deve permitir cadastro de alunos.
- **RF021** - O sistema deve permitir edição de dados cadastrais do aluno.
- **RF022** - O sistema deve permitir anexar contatos de emergência.
- **RF023** - O sistema deve permitir registrar observações médicas e restrições.
- **RF024** - O sistema deve permitir alterar status do aluno: ativo, inativo, suspenso, cancelado ou experimental.
- **RF025** - O sistema deve permitir consultar histórico completo do aluno.
- **RF026** - O sistema deve permitir importar alunos por planilha em fase futura.

### Gestão de professores

- **RF027** - O sistema deve permitir cadastro de professores.
- **RF028** - O sistema deve permitir vincular professor a uma ou mais turmas.
- **RF029** - O sistema deve permitir consultar agenda do professor.
- **RF030** - O sistema deve permitir registrar substituição de professor em aula específica.

### Gestão de turmas, horários e aulas

- **RF031** - O sistema deve permitir cadastro de turmas.
- **RF032** - O sistema deve permitir definir modalidade da turma.
- **RF033** - O sistema deve permitir definir professor principal da turma.
- **RF034** - O sistema deve permitir definir capacidade máxima da turma.
- **RF035** - O sistema deve permitir definir dias da semana e horários.
- **RF036** - O sistema deve permitir gerar aulas recorrentes a partir dos horários.
- **RF037** - O sistema deve permitir cancelar aula.
- **RF038** - O sistema deve permitir reagendar aula.
- **RF039** - O sistema deve permitir matricular aluno em turma.
- **RF040** - O sistema deve permitir remover aluno de turma.
- **RF041** - O sistema deve impedir matrícula acima da capacidade, salvo permissão administrativa.

### Presença e frequência

- **RF042** - O sistema deve permitir registro de presença por aula.
- **RF043** - O sistema deve permitir check-in pelo professor.
- **RF044** - O sistema deve permitir check-in pelo aluno com confirmação do professor, quando configurado.
- **RF045** - O sistema deve permitir registro de falta justificada.
- **RF046** - O sistema deve calcular percentual de frequência por aluno, turma, modalidade e período.
- **RF047** - O sistema deve calcular sequência de dias/aulas sem faltar.
- **RF048** - O sistema deve atualizar pontuação de presença após confirmação da aula.
- **RF049** - O sistema deve alimentar ranking interno por organização.

### Financeiro

- **RF050** - O sistema deve permitir cadastro de planos de mensalidade.
- **RF051** - O sistema deve permitir vincular aluno a um plano.
- **RF052** - O sistema deve permitir gerar mensalidades recorrentes.
- **RF053** - O sistema deve permitir registrar pagamento manual.
- **RF054** - O sistema deve permitir registrar desconto.
- **RF055** - O sistema deve permitir registrar multa e juros.
- **RF056** - O sistema deve permitir controlar vencimentos.
- **RF057** - O sistema deve permitir listar alunos inadimplentes.
- **RF058** - O sistema deve permitir marcar mensalidade como paga, pendente, vencida, cancelada ou isenta.
- **RF059** - O sistema deve gerar relatório financeiro por período.
- **RF060** - O sistema deve permitir exportação de relatórios em CSV/Excel em fase futura.
- **RF061** - O sistema deve estar preparado para futura integração com Pix, boleto e cartão.

### Graduação de artes marciais

- **RF062** - O sistema deve permitir cadastro de sistemas de graduação por modalidade.
- **RF063** - O sistema deve permitir cadastro de faixas, graus e ordem hierárquica.
- **RF064** - O sistema deve permitir registrar graduação atual do aluno.
- **RF065** - O sistema deve permitir registrar histórico de promoções.
- **RF066** - O sistema deve permitir informar professor responsável pela graduação.
- **RF067** - O sistema deve permitir configurar critérios mínimos para graduação: tempo, presença, aulas, idade mínima e avaliação manual.
- **RF068** - O sistema deve sugerir alunos elegíveis para graduação com base nos critérios configurados.
- **RF069** - O sistema deve permitir aprovar ou rejeitar sugestão de graduação.
- **RF070** - O sistema deve permitir emitir relatório de evolução marcial.

### Evolução de atividades físicas

- **RF071** - O sistema deve permitir cadastro de metas por aluno.
- **RF072** - O sistema deve permitir metas por dias de treino, frequência, carga, medidas, peso, condicionamento, pontuação ou categoria.
- **RF073** - O sistema deve permitir registrar progresso periódico.
- **RF074** - O sistema deve permitir definir categorias de evolução: Iniciante, Consistente, Evoluindo, Avançado e Referência.
- **RF075** - O sistema deve calcular pontuação por presença, metas concluídas e consistência.
- **RF076** - O sistema deve exibir evolução do aluno no app.
- **RF077** - O sistema deve permitir relatórios de evolução física.

### Gamificação

- **RF078** - O sistema deve manter ranking de frequência por modalidade, turma e organização.
- **RF079** - O sistema deve atribuir medalhas por marcos de presença.
- **RF080** - O sistema deve atribuir pontos por aula frequentada.
- **RF081** - O sistema deve atribuir pontos bônus por sequência sem faltas.
- **RF082** - O sistema deve permitir zerar ou arquivar rankings por período.
- **RF083** - O sistema deve permitir ocultar aluno do ranking por configuração de privacidade.

### Relatórios e dashboards

- **RF084** - O sistema deve disponibilizar dashboard administrativo.
- **RF085** - O sistema deve exibir alunos ativos, inativos, novos e cancelados.
- **RF086** - O sistema deve exibir presença média por turma.
- **RF087** - O sistema deve exibir receita prevista, recebida e em atraso.
- **RF088** - O sistema deve exibir evolução de alunos por modalidade.
- **RF089** - O sistema deve exibir ranking de alunos.
- **RF090** - O sistema deve permitir filtros por período, modalidade, turma, aluno e professor.

### Mobile

- **RF091** - O aplicativo mobile deve permitir login de aluno e professor.
- **RF092** - O aluno deve consultar presença, evolução, graduação, metas e pagamentos.
- **RF093** - O professor deve consultar agenda, turmas e registrar presença.
- **RF094** - O app deve exibir ranking e medalhas.
- **RF095** - O app deve receber notificações internas.

### Administração e auditoria

- **RF096** - O sistema deve registrar logs de auditoria para ações críticas.
- **RF097** - O sistema deve permitir consulta de auditoria por administradores autorizados.
- **RF098** - O sistema deve permitir parametrizações por organização.
- **RF099** - O sistema deve suportar soft delete para registros sensíveis.
- **RF100** - O sistema deve proteger dados por tenant/organização.

---

## 3. Requisitos Não Funcionais

### Segurança

- Autenticação com JWT de curta duração e refresh token rotacionável.
- Senhas armazenadas com hash forte, por exemplo Argon2id ou BCrypt com custo adequado.
- RBAC por organização, perfil e permissão.
- Validação de tenant em todas as consultas.
- Proteção contra OWASP Top 10.
- Criptografia em trânsito via HTTPS/TLS.
- Segredos fora do repositório, usando variáveis de ambiente no Coolify.
- Auditoria para eventos críticos.

### Performance

- Resposta média da API menor que 300 ms para operações comuns em condições normais.
- Paginação obrigatória em listagens.
- Índices em campos de busca, chaves estrangeiras e filtros por tenant.
- Cache Redis para dados de leitura frequente, como permissões e configurações.
- Dapper para consultas SQL performáticas e explícitas. (escritos em um record ou classe estatica a parte)

### Escalabilidade

- API stateless.
- Monólito modular preparado para extração futura.
- Separação clara entre API, Admin, Mobile, Infra, Banco e Docs.
- Projetos estruturados como futuros repositórios independentes.

### Disponibilidade, Backup, Observabilidade e Auditoria

- Health checks em API, Worker, PostgreSQL e Redis.
- Backup diário do PostgreSQL com teste de restauração.
- OpenTelemetry para traces, métricas e logs.
- Prometheus para métricas.
- Grafana para dashboards e alertas.
- Logs estruturados em JSON com correlation ID.
- Auditoria append-only lógica para ações críticas.

---

## 4. Casos de Uso

### UC001 - Cadastrar organização

**Ator:** Administrador SaaS ou dono da academia  
**Fluxo Principal:**
1. O ator informa dados da organização.
2. O sistema valida duplicidade de documento/e-mail.
3. O sistema cria a organização.
4. O sistema cria o usuário administrador inicial.
5. O sistema envia instruções de acesso.

**Fluxos Alternativos:**
- Documento já cadastrado: sistema bloqueia cadastro.
- E-mail inválido: sistema solicita correção.

### UC002 - Login

**Ator:** Aluno, professor, administrador, recepção ou financeiro  
**Fluxo Principal:**
1. O ator informa e-mail e senha.
2. O sistema valida credenciais.
3. O sistema identifica organizações e perfis vinculados.
4. O sistema emite JWT e refresh token.
5. O sistema registra auditoria de login.

**Fluxos Alternativos:**
- Senha incorreta: retornar 401.
- Usuário inativo: retornar 403.
- Usuário com múltiplas organizações: exigir seleção de contexto.

### UC003 - Gerenciar aluno

**Ator:** Administrador ou recepção  
**Fluxo Principal:**
1. O ator cadastra dados pessoais do aluno.
2. O ator seleciona modalidade(s).
3. O ator vincula turma(s) e plano financeiro.
4. O sistema cria matrícula e mensalidades conforme plano.
5. O sistema disponibiliza acesso ao app.

**Fluxos Alternativos:**
- Turma sem vaga: bloqueia ou solicita permissão administrativa.
- Plano ausente: permite cadastro sem financeiro, com alerta.

### UC004 - Gerenciar professor

**Ator:** Administrador  
**Fluxo Principal:**
1. O ator cadastra pessoa ou seleciona pessoa existente.
2. O ator adiciona perfil de professor.
3. O ator vincula modalidades e turmas.
4. O sistema atualiza permissões.

**Fluxos Alternativos:**
- Pessoa já é aluno: sistema mantém perfil aluno e adiciona professor.

### UC005 - Criar turma e horários

**Ator:** Administrador  
**Fluxo Principal:**
1. O ator informa nome, modalidade, professor e capacidade.
2. O ator define dias e horários.
3. O sistema gera grade recorrente.
4. O sistema disponibiliza a turma para matrícula.

**Fluxos Alternativos:**
- Conflito de horário do professor: sistema alerta e exige confirmação.
- Capacidade inválida: sistema bloqueia.

### UC006 - Registrar presença

**Ator:** Professor ou administrador  
**Fluxo Principal:**
1. O ator abre aula do dia.
2. O sistema lista alunos matriculados.
3. O ator marca presença, falta ou falta justificada.
4. O sistema salva chamada.
5. O sistema recalcula frequência, streak, pontuação e ranking.

**Fluxos Alternativos:**
- Aluno não matriculado: professor pode adicionar presença avulsa se permitido.
- Aula cancelada: sistema impede chamada.

### UC007 - Gerenciar pagamento

**Ator:** Financeiro ou administrador  
**Fluxo Principal:**
1. O ator consulta mensalidades.
2. O sistema exibe status e vencimentos.
3. O ator registra pagamento.
4. O sistema atualiza status para pago.
5. O sistema gera auditoria financeira.

**Fluxos Alternativos:**
- Pagamento parcial: sistema registra valor parcial e mantém saldo.
- Mensalidade vencida: sistema calcula multa/juros configurados.

### UC008 - Graduar aluno em arte marcial

**Ator:** Professor ou administrador  
**Fluxo Principal:**
1. O ator consulta aluno.
2. O sistema exibe graduação atual e critérios.
3. O sistema informa elegibilidade.
4. O ator registra nova faixa/grau.
5. O sistema grava histórico de graduação.

**Fluxos Alternativos:**
- Critérios não atendidos: sistema permite exceção apenas com permissão e justificativa.

### UC009 - Registrar meta e progresso físico

**Ator:** Professor ou administrador  
**Fluxo Principal:**
1. O ator cria meta para aluno.
2. O sistema define tipo, alvo, unidade e prazo.
3. O ator registra progresso periódico.
4. O sistema calcula percentual e pontuação.
5. O aluno visualiza evolução no app.

**Fluxos Alternativos:**
- Meta vencida: sistema marca como atrasada ou encerrada.

### UC010 - Consultar dashboard

**Ator:** Administrador  
**Fluxo Principal:**
1. O ator acessa dashboard.
2. O sistema calcula indicadores por período.
3. O sistema exibe alunos, presença, receita, inadimplência e ranking.

**Fluxos Alternativos:**
- Sem dados: sistema exibe tela inicial orientativa.

---

## 5. Arquitetura

### Decisão arquitetural

O Kiai Control será um **monólito modular** com Clean Architecture. O repositório inicial será monorepo temporário, mas os projetos serão isolados em pastas com nomes de futuros repositórios.

### Estrutura principal:

- KiaiControl.Api
- KiaiControl.Core
- KiaiControl.Contracts
- KiaiControl.UseCases
- KiaiControl.Repositories
- KiaiControl.Services
- Tests separados por tipo

## Dependências oficiais

KiaiControl.Api
 ├── KiaiControl.UseCases
 ├── KiaiControl.Contracts
 ├── KiaiControl.Repositories
 ├── KiaiControl.Services
 └── KiaiControl.Core

KiaiControl.UseCases
 ├── KiaiControl.Core
 ├── KiaiControl.Contracts
 ├── KiaiControl.Repositories
 └── KiaiControl.Services

KiaiControl.Repositories
 └── KiaiControl.Core

KiaiControl.Services
 └── KiaiControl.Core

KiaiControl.Contracts
 └── Sem dependências

KiaiControl.Core
 └── Sem dependências

## Fluxo de comunicação

Cliente
  -> API
  -> UseCases
      -> Repositories -> PostgreSQL
      -> Services
      -> Core

---

### Diagrama textual

```text
[Blazor Server Admin] ---- HTTPS/JWT ----+
                                         |
[Flutter Mobile App] ---- HTTPS/JWT -----+--> [.NET 10 Web API]
                                                    |
                                                    v

KiaiControl.sln
│
├── README.md
├── SYSTEM_DESIGN.md
├── .editorconfig
├── .gitignore
│
├── docs/
│   ├── API_CONTRACT_PT.md
│   ├── API_CONTRACT_EN.md
│   ├── WORKFLOW.md
│   ├── DATABASE.md
│   ├── DEPLOYMENT.md
│   └── DECISIONS.md
│
├── database/
│   ├── migrations/
│   ├── seeds/
│   ├── scripts/
│   └── views/
│
├── infra/
│   ├── docker/
│   ├── environments/
│   ├── monitoring/
│   └── pipelines/
│
├── src/
│
│   ├── KiaiControl.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── OrganizationsController.cs
│   │   │   ├── StudentsController.cs
│   │   │   ├── TeachersController.cs
│   │   │   ├── ModalitiesController.cs
│   │   │   ├── ClassesController.cs
│   │   │   ├── AttendanceController.cs
│   │   │   ├── BillingController.cs
│   │   │   ├── GraduationController.cs
│   │   │   ├── FitnessController.cs
│   │   │   ├── RankingController.cs
│   │   │   ├── NotificationsController.cs
│   │   │   ├── ReportsController.cs
│   │   │   └── DashboardController.cs
│   │   │
│   │   ├── Filters/
│   │   ├── Middlewares/
│   │   ├── Extensions/
│   │   ├── Auth/
│   │   ├── OpenApi/
│   │   ├── Configuration/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Staging.json
│   │   └── appsettings.Production.json
│   │
│   ├── KiaiControl.Core/
│   │   ├── Entities/
│   │   │   ├── Organization.cs
│   │   │   ├── User.cs
│   │   │   ├── Student.cs
│   │   │   ├── Teacher.cs
│   │   │   ├── Modality.cs
│   │   │   ├── ClassGroup.cs
│   │   │   ├── Lesson.cs
│   │   │   ├── Attendance.cs
│   │   │   ├── Graduation.cs
│   │   │   ├── FitnessGoal.cs
│   │   │   ├── Invoice.cs
│   │   │   ├── Payment.cs
│   │   │   ├── RankingEntry.cs
│   │   │   ├── Notification.cs
│   │   │   ├── AuditLog.cs
│   │   │   └── DashboardMetrics.cs
│   │   │
│   │   ├── Enums/
│   │   ├── Constants/
│   │   ├── Exceptions/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   └── Common/
│   │
│   ├── KiaiControl.Contracts/
│   │   ├── Requests/
│   │   │
│   │   ├── Responses/
│   │   │
│   │   ├── Auth/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── LoginResponse.cs
│   │   │   ├── RefreshTokenRequest.cs
│   │   │   └── RefreshTokenResponse.cs
│   │   │
│   │   ├── Students/
│   │   │   ├── CreateStudentRequest.cs
│   │   │   ├── UpdateStudentRequest.cs
│   │   │   ├── StudentResponse.cs
│   │   │   └── StudentSummaryResponse.cs
│   │   │
│   │   ├── Attendance/
│   │   ├── Billing/
│   │   ├── Graduation/
│   │   ├── Fitness/
│   │   ├── Ranking/
│   │   ├── Notifications/
│   │   ├── Reports/
│   │   └── Dashboard/
│   │
│   ├── KiaiControl.UseCases/
│   │   ├── Auth/
│   │   │   ├── Login/
│   │   │   ├── RefreshToken/
│   │   │   └── ChangePassword/
│   │   │
│   │   ├── Organizations/
│   │   │
│   │   ├── Students/
│   │   │   ├── CreateStudent/
│   │   │   ├── UpdateStudent/
│   │   │   ├── DeleteStudent/
│   │   │   ├── GetStudent/
│   │   │   └── ListStudents/
│   │   │
│   │   ├── Teachers/
│   │   ├── Modalities/
│   │   ├── Classes/
│   │   ├── Attendance/
│   │   │   ├── RegisterAttendance/
│   │   │   ├── RegisterAbsence/
│   │   │   └── AttendanceReport/
│   │   │
│   │   ├── Billing/
│   │   │   ├── GenerateInvoice/
│   │   │   ├── RegisterPayment/
│   │   │   ├── CancelInvoice/
│   │   │   └── FinancialDashboard/
│   │   │
│   │   ├── Graduation/
│   │   ├── Fitness/
│   │   ├── Ranking/
│   │   ├── Notifications/
│   │   ├── Reports/
│   │   └── Dashboard/
│   │
│   ├── KiaiControl.Repositories/
│   │   ├── Connections/
│   │   │   └── DbConnectionFactory.cs
│   │   │
│   │   ├── Repositories/
│   │   │   ├── UserRepository.cs
│   │   │   ├── StudentRepository.cs
│   │   │   ├── TeacherRepository.cs
│   │   │   ├── AttendanceRepository.cs
│   │   │   ├── BillingRepository.cs
│   │   │   ├── GraduationRepository.cs
│   │   │   ├── RankingRepository.cs
│   │   │   └── DashboardRepository.cs
│   │   │
│   │   ├── Queries/
│   │   │   ├── Students/
│   │   │   ├── Attendance/
│   │   │   ├── Billing/
│   │   │   ├── Ranking/
│   │   │   ├── Dashboard/
│   │   │   └── Reports/
│   │   │
│   │   ├── Sql/
│   │   │   ├── Students/
│   │   │   ├── Attendance/
│   │   │   ├── Billing/
│   │   │   ├── Ranking/
│   │   │   └── Reports/
│   │   │
│   │   ├── Migrations/
│   │   └── Transactions/
│   │
│   ├── KiaiControl.Services/
│   │   ├── Auth/
│   │   │   ├── JwtService.cs
│   │   │   ├── PasswordHasherService.cs
│   │   │   └── RefreshTokenService.cs
│   │   │
│   │   ├── Email/
│   │   │
│   │   ├── Cache/
│   │   │
│   │   ├── Storage/
│   │   │
│   │   ├── Notifications/
│   │   │
│   │   ├── Telemetry/
│   │   │
│   │   ├── Integrations/
│   │   │   ├── PaymentGateway/
│   │   │   ├── WhatsApp/
│   │   │   └── ExternalApis/
│   │   │
│   │   └── Jobs/
│   │       ├── GenerateInvoicesJob.cs
│   │       ├── OverdueInvoicesJob.cs
│   │       ├── GenerateLessonsJob.cs
│   │       ├── RankingRecalculationJob.cs
│   │       └── NotificationJob.cs
│
└── tests/
    │
    ├── KiaiControl.UnitTests/
    │   ├── UseCases/
    │   ├── Services/
    │   ├── Validators/
    │   └── Core/
    │
    ├── KiaiControl.IntegrationTests/
    │   ├── Controllers/
    │   ├── Repositories/
    │   ├── Database/
    │   └── Fixtures/
    │
    ├── KiaiControl.ArchitectureTests/
    │   ├── Layers/
    │   ├── Naming/
    │   └── Dependencies/
    │
    └── KiaiControl.PerformanceTests/
        ├── K6/
        ├── Login/
        ├── Attendance/
        ├── Billing/
        └── Dashboard/

```

```text

KiaiControl.Api
    ├── KiaiControl.UseCases
    ├── KiaiControl.Contracts
    ├── KiaiControl.Services
    ├── KiaiControl.Repositories
    └── KiaiControl.Core

KiaiControl.UseCases
    ├── KiaiControl.Core
    ├── KiaiControl.Contracts
    ├── KiaiControl.Repositories
    └── KiaiControl.Services

KiaiControl.Repositories
    └── KiaiControl.Core

KiaiControl.Services
    └── KiaiControl.Core

KiaiControl.Contracts
    └── (sem dependências)

KiaiControl.Core
    └── (sem dependências)

```

### Serviços/módulos

- Identity & Access.
- Organizations.
- People.
- Students.
- Teachers.
- Modalities.
- Classes.
- Attendance.
- Billing.
- Martial Progression.
- Fitness Progression.
- Gamification.
- Reports.
- Notifications.
- Audit.
### APIs

- API REST JSON para Web e Mobile.
- Futuro suporte a webhooks para integrações.
- Swagger/OpenAPI publicado por ambiente.
- Versionamento por rota: `/api/v1`.

### Fluxo de comunicação

```text
Cliente -> API -> Domínio -> UseCases -> Repositório Dapper -> PostgreSQL
                              |                         |
                              |                         +-> Redis quando aplicável
                              +-> OpenTelemetry -> Prometheus/Grafana
```

---

## 6. Tecnologias

### Frontend

- .NET 10 Blazor Server.
- MudBlazor.

### API

- .NET 10 Web API.
- ASP.NET Core 10.
- Dapper.

### Autenticação

- JWT + Refresh Token.

### Mobile

- Flutter/Dart.

### Banco

- PostgreSQL estável suportado mais recente no provisionamento.

### Cache

- Redis.

### Observabilidade

- OpenTelemetry.
- Prometheus.
- Grafana OSS.

### CI/CD e Infraestrutura

- GitHub Actions.
- Coolify.
- Hostinger VPS.
- Docker.
- Docker Compose.
- Kubernetes apenas se necessário no futuro.

---

## 7. Linguagem e Framework

- **Frontend Admin:** Blazor Server .NET 10 + MudBlazor pela produtividade em painéis administrativos e integração com C#.
- **Backend:** .NET 10 Web API por performance, LTS, segurança e ecossistema.
- **ORM/Data Access:** Dapper por SQL explícito, performance e previsibilidade.
- **Mobile:** Flutter por base única Android/iOS.
- **Arquitetura:** Clean Architecture, DDD, CQRS, Repository Pattern e SOLID.

---

## 8. Estrutura do Projeto

A estrutura inicial será em **monorepo apenas para bootstrap arquitetural, padronização e contexto comum assistido por IA**. A decisão estratégica é que cada aplicação nasça dentro de uma pasta com nome de repositório futuro, contendo sua própria solução, documentação e README. Assim, quando o produto amadurecer, será possível extrair cada projeto para um repositório independente com baixo atrito.

### Princípios da estrutura

- Cada projeto principal deve possuir seu próprio `README.md`.
- Cada projeto deve ter sua própria solution quando fizer sentido, como API e Admin.
- Os nomes das pastas devem representar o nome futuro do repositório.
- O monorepo inicial não deve criar acoplamento artificial entre projetos.
- Contratos compartilhados devem ser publicados via OpenAPI, e não compartilhados por dependência direta com frontend/mobile.
- A documentação raiz mantém o contexto geral do produto.
- A documentação interna de cada projeto explica como executar, testar, publicar e evoluir aquele projeto isoladamente.

### Estrutura proposta do monorepo inicial

```text
kiai-control/
├── README.md
├── SYSTEM_DESIGN.md
├── .editorconfig
├── .gitignore
├── .github/
│   └── workflows/
│       ├── backend-api-ci.yml
│       ├── frontend-admin-ci.yml
│       ├── mobile-app-ci.yml
│       ├── infra-ci.yml
│       └── deploy.yml
│
├── backend/
│   └── kiai-control-api/
│       ├── README.md
│       ├── KiaiControl.Api.sln
│       ├── Directory.Build.props
│       ├── Directory.Packages.props
│       ├── src/
│       │   ├── KiaiControl.Api/
│       │   │   ├── Controllers/
│       │   │   ├── Filters/
│       │   │   ├── Middlewares/
│       │   │   ├── Extensions/
│       │   │   ├── Auth/
│       │   │   ├── OpenApi/
│       │   │   ├── Configuration/
│       │   │   ├── Program.cs
│       │   │   ├── appsettings.json
│       │   │   ├── appsettings.Development.json
│       │   │   ├── appsettings.Staging.json
│       │   │   └── appsettings.Production.json
│       │   ├── KiaiControl.Core/
│       │   │   ├── Entities/
│       │   │   ├── Enums/
│       │   │   ├── Constants/
│       │   │   ├── Exceptions/
│       │   │   ├── ValueObjects/
│       │   │   ├── Events/
│       │   │   └── Common/
│       │   ├── KiaiControl.Contracts/
│       │   │   ├── Requests/
│       │   │   ├── Responses/
│       │   │   ├── Auth/
│       │   │   ├── Students/
│       │   │   ├── Attendance/
│       │   │   ├── Billing/
│       │   │   ├── Graduation/
│       │   │   ├── Fitness/
│       │   │   ├── Ranking/
│       │   │   ├── Notifications/
│       │   │   ├── Reports/
│       │   │   └── Dashboard/
│       │   ├── KiaiControl.UseCases/
│       │   │   ├── Auth/
│       │   │   ├── Organizations/
│       │   │   ├── Students/
│       │   │   ├── Teachers/
│       │   │   ├── Modalities/
│       │   │   ├── Classes/
│       │   │   ├── Attendance/
│       │   │   ├── Billing/
│       │   │   ├── Graduation/
│       │   │   ├── Fitness/
│       │   │   ├── Ranking/
│       │   │   ├── Notifications/
│       │   │   ├── Reports/
│       │   │   └── Dashboard/
│       │   ├── KiaiControl.Repositories/
│       │   │   ├── Connections/
│       │   │   ├── Repositories/
│       │   │   ├── Queries/
│       │   │   ├── Sql/
│       │   │   ├── Migrations/
│       │   │   └── Transactions/
│       │   └── KiaiControl.Services/
│       │       ├── Auth/
│       │       ├── Email/
│       │       ├── Cache/
│       │       ├── Storage/
│       │       ├── Notifications/
│       │       ├── Telemetry/
│       │       ├── Integrations/
│       │       └── Jobs/
│       └── tests/
│           ├── README.md
│           ├── KiaiControl.UnitTests/
│           │   ├── UseCases/
│           │   ├── Services/
│           │   ├── Validators/
│           │   └── Core/
│           ├── KiaiControl.IntegrationTests/
│           │   ├── Controllers/
│           │   ├── Repositories/
│           │   ├── Database/
│           │   └── Fixtures/
│           ├── KiaiControl.ArchitectureTests/
│           │   ├── Layers/
│           │   ├── Naming/
│           │   └── Dependencies/
│           └── KiaiControl.PerformanceTests/
│               ├── K6/
│               ├── Login/
│               ├── Attendance/
│               ├── Billing/
│               └── Dashboard/
│
├── frontend/
│   └── kiai-control-admin/
│       ├── README.md
│       ├── KiaiControl.Admin.sln
│       ├── Directory.Build.props
│       ├── Directory.Packages.props
│       ├── src/
│       │   └── KiaiControl.Admin/
│       │       ├── Components/
│       │       ├── Pages/
│       │       ├── Layout/
│       │       ├── Services/
│       │       ├── Clients/
│       │       ├── Auth/
│       │       ├── State/
│       │       ├── Themes/
│       │       ├── wwwroot/
│       │       ├── appsettings.json
│       │       ├── appsettings.Development.json
│       │       └── Program.cs
│       └── tests/
│           ├── KiaiControl.Admin.UnitTests/
│           │   ├── README.md
│           │   ├── Components/
│           │   ├── Services/
│           │   └── KiaiControl.Admin.UnitTests.csproj
│           └── KiaiControl.Admin.E2ETests/
│               ├── Playwright/
│               └── KiaiControl.Admin.E2ETests.csproj
│
├── mobile/
│   └── kiai-control-app/
│       ├── README.md
│       ├── pubspec.yaml
│       ├── analysis_options.yaml
│       ├── lib/
│       │   ├── main.dart
│       │   ├── core/
│       │   │   ├── config/
│       │   │   ├── http/
│       │   │   ├── storage/
│       │   │   ├── routing/
│       │   │   ├── theme/
│       │   │   └── widgets/
│       │   └── features/
│       │       ├── auth/
│       │       ├── dashboard/
│       │       ├── attendance/
│       │       ├── payments/
│       │       ├── graduation/
│       │       ├── fitness_progress/
│       │       ├── ranking/
│       │       └── teacher_classes/
│       ├── test/
│       │   ├── unit/
│       │   └── widget/
│       └── integration_test/
│           └── app_flow_test.dart
│
├── infra/
│   └── kiai-control-infra/
│       ├── README.md
│       ├── docker/
│       │   ├── api.Dockerfile
│       │   ├── admin.Dockerfile
│       │   └── worker.Dockerfile
│       ├── compose/
│       │   ├── docker-compose.yml
│       │   ├── docker-compose.override.yml
│       │   ├── docker-compose.staging.yml
│       │   └── docker-compose.prod.yml
│       ├── coolify/
│       │   └── services.md
│       ├── observability/
│       │   ├── prometheus.yml
│       │   ├── otel-collector.yml
│       │   └── grafana-dashboards/
│       ├── nginx/
│       │   └── nginx.conf
│       └── scripts/
│           ├── backup-postgres.sh
│           └── restore-postgres.sh
│
├── database/
│   └── kiai-control-database/
│       ├── README.md
│       ├── dbml/
│       │   └── kiai_control.dbml
│       ├── migrations/
│       ├── seeds/
│       └── scripts/
│
└── docs/
    └── kiai-control-docs/
        ├── README.md
        ├── architecture/
        ├── api/
        ├── deploy/
        ├── operations/
        ├── decisions/
        └── product/
```

### READMEs obrigatórios por projeto

Cada projeto preparado para virar repositório próprio deve ter um `README.md` na raiz:

```text
backend/kiai-control-api/README.md
frontend/kiai-control-admin/README.md
mobile/kiai-control-app/README.md
infra/kiai-control-infra/README.md
database/kiai-control-database/README.md
docs/kiai-control-docs/README.md
```

### Template mínimo para README de cada projeto

```markdown
# Nome do Projeto

## Objetivo
Descrever o papel deste projeto dentro do Kiai Control.

## Responsabilidades
- Listar responsabilidades principais.
- Listar o que este projeto não deve fazer.

## Stack
- Linguagem/framework.
- Bibliotecas principais.
- Dependências externas.

## Como executar localmente

```bash
# comandos do projeto
```

## Como testar

```bash
# comandos de teste
```

## Variáveis de ambiente
Listar as variáveis necessárias sem expor segredos reais.

## Estrutura interna
Explicar as principais pastas.

## Padrões de código
Explicar convenções, arquitetura e regras de dependência.

## CI/CD
Explicar como este projeto é validado e publicado.

## Observabilidade
Explicar logs, métricas e traces aplicáveis.

## Decisões importantes
Linkar ADRs ou decisões específicas.
```

---

## 9. Modelagem do Banco de Dados

### DER textual

```text
organizations 1---N branches
organizations 1---N persons
persons 1---N user_accounts
persons N---N roles via person_roles
organizations 1---N modalities
persons N---N modalities via person_modalities
modalities 1---N class_groups
class_groups 1---N schedules
class_groups 1---N lessons
persons N---N class_groups via enrollments
lessons 1---N attendances
persons 1---N attendances
organizations 1---N plans
persons 1---N student_plan_subscriptions
student_plan_subscriptions 1---N invoices
invoices 1---N payments
modalities 1---N graduation_systems
graduation_systems 1---N graduation_levels
persons 1---N student_graduations
persons 1---N graduation_events
persons 1---N fitness_goals
fitness_goals 1---N fitness_progress_records
organizations 1---N ranking_cycles
ranking_cycles 1---N ranking_entries
persons N---N medals via person_medals
organizations 1---N audit_logs
```

### DBML

```dbml
Project kiai_control {
  database_type: 'PostgreSQL'
  Note: 'Modelo inicial do Kiai Control'
}

Enum organization_status {
  active
  inactive
  suspended
}

Enum person_status {
  active
  inactive
  suspended
  cancelled
  trial
}

Enum role_code {
  student
  teacher
  admin
  finance
  receptionist
  owner
}

Enum modality_type {
  martial_art
  fitness_activity
  hybrid
}

Enum lesson_status {
  scheduled
  completed
  cancelled
  rescheduled
}

Enum attendance_status {
  present
  absent
  justified_absence
  late
}

Enum invoice_status {
  pending
  paid
  overdue
  cancelled
  exempt
  partially_paid
}

Enum payment_method {
  cash
  pix
  credit_card
  debit_card
  bank_transfer
  manual_adjustment
}

Enum goal_type {
  training_days
  attendance_rate
  weight
  body_measurement
  load
  conditioning
  points
  custom
}

Enum goal_status {
  active
  completed
  cancelled
  expired
}

Table organizations {
  id uuid [pk]
  name varchar(160) [not null]
  trade_name varchar(160)
  document varchar(30)
  email varchar(160)
  phone varchar(30)
  status organization_status [not null, default: 'active']
  created_at timestamptz [not null]
  updated_at timestamptz
  deleted_at timestamptz

  Indexes {
    document [unique]
    email
    status
  }
}

Table branches {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  name varchar(160) [not null]
  address_line varchar(255)
  city varchar(100)
  state varchar(50)
  postal_code varchar(20)
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    (organization_id, name) [unique]
  }
}

Table persons {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  full_name varchar(180) [not null]
  preferred_name varchar(100)
  email varchar(160)
  phone varchar(30)
  birth_date date
  document varchar(30)
  status person_status [not null, default: 'active']
  medical_notes text
  emergency_contact_name varchar(160)
  emergency_contact_phone varchar(30)
  hide_from_ranking boolean [not null, default: false]
  created_at timestamptz [not null]
  updated_at timestamptz
  deleted_at timestamptz

  Indexes {
    organization_id
    (organization_id, email)
    (organization_id, document)
    (organization_id, status)
    (organization_id, full_name)
  }
}

Table user_accounts {
  id uuid [pk]
  person_id uuid [not null, ref: > persons.id]
  email varchar(160) [not null]
  password_hash varchar(255) [not null]
  is_active boolean [not null, default: true]
  last_login_at timestamptz
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    email [unique]
    person_id
  }
}

Table refresh_tokens {
  id uuid [pk]
  user_account_id uuid [not null, ref: > user_accounts.id]
  token_hash varchar(255) [not null]
  expires_at timestamptz [not null]
  revoked_at timestamptz
  replaced_by_token_id uuid [ref: > refresh_tokens.id]
  created_at timestamptz [not null]
  created_by_ip varchar(80)

  Indexes {
    user_account_id
    token_hash [unique]
    expires_at
  }
}

Table roles {
  id uuid [pk]
  code role_code [not null]
  name varchar(80) [not null]

  Indexes {
    code [unique]
  }
}

Table permissions {
  id uuid [pk]
  code varchar(120) [not null]
  description varchar(255)

  Indexes {
    code [unique]
  }
}

Table role_permissions {
  role_id uuid [not null, ref: > roles.id]
  permission_id uuid [not null, ref: > permissions.id]

  Indexes {
    (role_id, permission_id) [pk]
  }
}

Table person_roles {
  person_id uuid [not null, ref: > persons.id]
  role_id uuid [not null, ref: > roles.id]
  organization_id uuid [not null, ref: > organizations.id]
  created_at timestamptz [not null]

  Indexes {
    (person_id, role_id, organization_id) [pk]
    organization_id
  }
}

Table modalities {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  name varchar(120) [not null]
  type modality_type [not null]
  description text
  is_active boolean [not null, default: true]
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    (organization_id, name) [unique]
    type
  }
}

Table person_modalities {
  person_id uuid [not null, ref: > persons.id]
  modality_id uuid [not null, ref: > modalities.id]
  organization_id uuid [not null, ref: > organizations.id]
  started_at date
  ended_at date
  is_active boolean [not null, default: true]

  Indexes {
    (person_id, modality_id) [pk]
    organization_id
    modality_id
  }
}

Table class_groups {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  branch_id uuid [ref: > branches.id]
  modality_id uuid [not null, ref: > modalities.id]
  name varchar(120) [not null]
  main_teacher_id uuid [ref: > persons.id]
  capacity int [not null]
  is_active boolean [not null, default: true]
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    modality_id
    main_teacher_id
    (organization_id, name)
  }
}

Table class_schedules {
  id uuid [pk]
  class_group_id uuid [not null, ref: > class_groups.id]
  day_of_week int [not null]
  starts_at time [not null]
  ends_at time [not null]
  created_at timestamptz [not null]

  Indexes {
    class_group_id
    (class_group_id, day_of_week, starts_at) [unique]
  }
}

Table enrollments {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  class_group_id uuid [not null, ref: > class_groups.id]
  enrolled_at date [not null]
  cancelled_at date
  is_active boolean [not null, default: true]

  Indexes {
    organization_id
    person_id
    class_group_id
    (person_id, class_group_id, is_active)
  }
}

Table lessons {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  class_group_id uuid [not null, ref: > class_groups.id]
  teacher_id uuid [ref: > persons.id]
  lesson_date date [not null]
  starts_at time [not null]
  ends_at time [not null]
  status lesson_status [not null, default: 'scheduled']
  cancellation_reason text
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    class_group_id
    teacher_id
    (class_group_id, lesson_date, starts_at) [unique]
    (organization_id, lesson_date)
  }
}

Table attendances {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  lesson_id uuid [not null, ref: > lessons.id]
  person_id uuid [not null, ref: > persons.id]
  status attendance_status [not null]
  checked_by_person_id uuid [ref: > persons.id]
  note text
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    lesson_id
    person_id
    (lesson_id, person_id) [unique]
    (organization_id, person_id, created_at)
  }
}

Table plans {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  name varchar(120) [not null]
  monthly_amount numeric(12,2) [not null]
  due_day int [not null]
  is_active boolean [not null, default: true]
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    (organization_id, name) [unique]
  }
}

Table student_plan_subscriptions {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  plan_id uuid [not null, ref: > plans.id]
  starts_on date [not null]
  ends_on date
  is_active boolean [not null, default: true]
  created_at timestamptz [not null]

  Indexes {
    organization_id
    person_id
    plan_id
  }
}

Table invoices {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  subscription_id uuid [not null, ref: > student_plan_subscriptions.id]
  person_id uuid [not null, ref: > persons.id]
  reference_month date [not null]
  due_date date [not null]
  amount numeric(12,2) [not null]
  discount_amount numeric(12,2) [not null, default: 0]
  fine_amount numeric(12,2) [not null, default: 0]
  interest_amount numeric(12,2) [not null, default: 0]
  paid_amount numeric(12,2) [not null, default: 0]
  status invoice_status [not null, default: 'pending']
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    person_id
    due_date
    status
    (subscription_id, reference_month) [unique]
  }
}

Table payments {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  invoice_id uuid [not null, ref: > invoices.id]
  amount numeric(12,2) [not null]
  method payment_method [not null]
  paid_at timestamptz [not null]
  external_reference varchar(120)
  note text
  created_by_person_id uuid [ref: > persons.id]
  created_at timestamptz [not null]

  Indexes {
    organization_id
    invoice_id
    paid_at
  }
}

Table graduation_systems {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  modality_id uuid [not null, ref: > modalities.id]
  name varchar(120) [not null]
  created_at timestamptz [not null]

  Indexes {
    organization_id
    modality_id
    (modality_id, name) [unique]
  }
}

Table graduation_levels {
  id uuid [pk]
  graduation_system_id uuid [not null, ref: > graduation_systems.id]
  name varchar(80) [not null]
  color varchar(40)
  degree int [not null, default: 0]
  sort_order int [not null]
  min_training_days int
  min_attendance_count int
  min_age int
  created_at timestamptz [not null]

  Indexes {
    graduation_system_id
    (graduation_system_id, sort_order) [unique]
  }
}

Table student_graduations {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  modality_id uuid [not null, ref: > modalities.id]
  current_level_id uuid [not null, ref: > graduation_levels.id]
  achieved_at date [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    person_id
    modality_id
    (person_id, modality_id) [unique]
  }
}

Table graduation_events {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  modality_id uuid [not null, ref: > modalities.id]
  from_level_id uuid [ref: > graduation_levels.id]
  to_level_id uuid [not null, ref: > graduation_levels.id]
  promoted_by_person_id uuid [ref: > persons.id]
  promoted_at date [not null]
  note text
  created_at timestamptz [not null]

  Indexes {
    organization_id
    person_id
    modality_id
    promoted_at
  }
}

Table fitness_goals {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  modality_id uuid [ref: > modalities.id]
  title varchar(160) [not null]
  type goal_type [not null]
  target_value numeric(12,2) [not null]
  current_value numeric(12,2) [not null, default: 0]
  unit varchar(40)
  starts_on date [not null]
  due_on date
  status goal_status [not null, default: 'active']
  created_by_person_id uuid [ref: > persons.id]
  created_at timestamptz [not null]
  updated_at timestamptz

  Indexes {
    organization_id
    person_id
    modality_id
    status
  }
}

Table fitness_progress_records {
  id uuid [pk]
  goal_id uuid [not null, ref: > fitness_goals.id]
  value numeric(12,2) [not null]
  recorded_at timestamptz [not null]
  note text
  recorded_by_person_id uuid [ref: > persons.id]

  Indexes {
    goal_id
    recorded_at
  }
}

Table ranking_cycles {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  name varchar(120) [not null]
  starts_on date [not null]
  ends_on date [not null]
  is_active boolean [not null, default: true]
  created_at timestamptz [not null]

  Indexes {
    organization_id
    (organization_id, starts_on, ends_on)
  }
}

Table ranking_entries {
  id uuid [pk]
  ranking_cycle_id uuid [not null, ref: > ranking_cycles.id]
  person_id uuid [not null, ref: > persons.id]
  modality_id uuid [ref: > modalities.id]
  points int [not null, default: 0]
  attendance_count int [not null, default: 0]
  current_streak int [not null, default: 0]
  best_streak int [not null, default: 0]
  updated_at timestamptz

  Indexes {
    ranking_cycle_id
    person_id
    modality_id
    points
    (ranking_cycle_id, person_id, modality_id) [unique]
  }
}

Table medals {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  name varchar(120) [not null]
  description varchar(255)
  required_points int
  required_streak int
  required_attendance_count int
  icon varchar(80)
  created_at timestamptz [not null]

  Indexes {
    organization_id
    (organization_id, name) [unique]
  }
}

Table person_medals {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  medal_id uuid [not null, ref: > medals.id]
  awarded_at timestamptz [not null]

  Indexes {
    organization_id
    person_id
    medal_id
    (person_id, medal_id) [unique]
  }
}

Table notifications {
  id uuid [pk]
  organization_id uuid [not null, ref: > organizations.id]
  person_id uuid [not null, ref: > persons.id]
  title varchar(160) [not null]
  message text [not null]
  read_at timestamptz
  created_at timestamptz [not null]

  Indexes {
    organization_id
    person_id
    read_at
  }
}

Table audit_logs {
  id uuid [pk]
  organization_id uuid [ref: > organizations.id]
  actor_person_id uuid [ref: > persons.id]
  action varchar(120) [not null]
  entity_name varchar(120) [not null]
  entity_id uuid
  ip_address varchar(80)
  user_agent text
  correlation_id varchar(120)
  metadata jsonb
  created_at timestamptz [not null]

  Indexes {
    organization_id
    actor_person_id
    action
    entity_name
    entity_id
    created_at
  }
}
```

### Constraints adicionais recomendadas

- `plans.monthly_amount >= 0`.
- `plans.due_day between 1 and 28` para simplificar recorrência mensal.
- `class_groups.capacity > 0`.
- `class_schedules.day_of_week between 0 and 6`.
- `class_schedules.ends_at > class_schedules.starts_at`.
- `invoices.amount >= 0`.
- `payments.amount > 0`.
- `fitness_goals.target_value > 0`.
- Todas as queries devem filtrar por `organization_id` quando a tabela for multi-tenant.

---

## 10. Dicionário de Dados

### organizations

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador da organização. |
| name | varchar(160) | Sim | Razão social ou nome principal. |
| trade_name | varchar(160) | Não | Nome fantasia. |
| document | varchar(30) | Não | CNPJ/CPF ou documento equivalente. |
| email | varchar(160) | Não | E-mail principal. |
| phone | varchar(30) | Não | Telefone principal. |
| status | enum | Sim | Status da organização. |
| created_at | timestamptz | Sim | Data de criação. |
| updated_at | timestamptz | Não | Data de atualização. |
| deleted_at | timestamptz | Não | Exclusão lógica. |

### branches

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador da unidade. |
| organization_id | uuid | Sim | Organização proprietária. |
| name | varchar(160) | Sim | Nome da unidade. |
| address_line | varchar(255) | Não | Endereço. |
| city | varchar(100) | Não | Cidade. |
| state | varchar(50) | Não | Estado. |
| postal_code | varchar(20) | Não | CEP. |
| created_at | timestamptz | Sim | Criação. |
| updated_at | timestamptz | Não | Atualização. |

### persons

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador da pessoa. |
| organization_id | uuid | Sim | Organização. |
| full_name | varchar(180) | Sim | Nome completo. |
| preferred_name | varchar(100) | Não | Nome preferido. |
| email | varchar(160) | Não | E-mail. |
| phone | varchar(30) | Não | Telefone. |
| birth_date | date | Não | Data de nascimento. |
| document | varchar(30) | Não | Documento. |
| status | enum | Sim | Status do aluno/pessoa. |
| medical_notes | text | Não | Observações médicas. |
| emergency_contact_name | varchar(160) | Não | Contato de emergência. |
| emergency_contact_phone | varchar(30) | Não | Telefone de emergência. |
| hide_from_ranking | boolean | Sim | Define privacidade em rankings. |
| created_at | timestamptz | Sim | Criação. |
| updated_at | timestamptz | Não | Atualização. |
| deleted_at | timestamptz | Não | Exclusão lógica. |

### user_accounts

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador da conta. |
| person_id | uuid | Sim | Pessoa vinculada. |
| email | varchar(160) | Sim | Login. |
| password_hash | varchar(255) | Sim | Hash da senha. |
| is_active | boolean | Sim | Indica se pode autenticar. |
| last_login_at | timestamptz | Não | Último login. |
| created_at | timestamptz | Sim | Criação. |
| updated_at | timestamptz | Não | Atualização. |

### refresh_tokens

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador. |
| user_account_id | uuid | Sim | Conta de usuário. |
| token_hash | varchar(255) | Sim | Hash do refresh token. |
| expires_at | timestamptz | Sim | Expiração. |
| revoked_at | timestamptz | Não | Revogação. |
| replaced_by_token_id | uuid | Não | Token substituto. |
| created_at | timestamptz | Sim | Criação. |
| created_by_ip | varchar(80) | Não | IP de origem. |

### roles, permissions, role_permissions e person_roles

| Tabela | Descrição |
|---|---|
| roles | Papéis disponíveis, como aluno, professor e administrador. |
| permissions | Permissões granulares do sistema. |
| role_permissions | Associação entre papéis e permissões. |
| person_roles | Papéis atribuídos para uma pessoa em uma organização. |

### modalities

| Campo | Tipo | Obrigatório | Descrição |
|---|---:|:---:|---|
| id | uuid | Sim | Identificador. |
| organization_id | uuid | Sim | Organização. |
| name | varchar(120) | Sim | Nome da modalidade. |
| type | enum | Sim | Arte marcial, atividade física ou híbrida. |
| description | text | Não | Descrição. |
| is_active | boolean | Sim | Status. |
| created_at | timestamptz | Sim | Criação. |
| updated_at | timestamptz | Não | Atualização. |

### class_groups, class_schedules, enrollments, lessons e attendances

| Tabela | Descrição |
|---|---|
| class_groups | Turmas por modalidade, professor e capacidade. |
| class_schedules | Grade semanal recorrente da turma. |
| enrollments | Matrículas de alunos nas turmas. |
| lessons | Aulas geradas ou cadastradas. |
| attendances | Presenças/faltas dos alunos em cada aula. |

### plans, subscriptions, invoices e payments

| Tabela | Descrição |
|---|---|
| plans | Planos financeiros da academia. |
| student_plan_subscriptions | Assinatura/plano do aluno. |
| invoices | Mensalidades geradas. |
| payments | Pagamentos registrados. |

### graduation_systems, graduation_levels, student_graduations e graduation_events

| Tabela | Descrição |
|---|---|
| graduation_systems | Sistema de graduação de uma modalidade. |
| graduation_levels | Faixas/graus ordenados com critérios mínimos. |
| student_graduations | Graduação atual do aluno por modalidade. |
| graduation_events | Histórico de promoções. |

### fitness_goals e fitness_progress_records

| Tabela | Descrição |
|---|---|
| fitness_goals | Metas físicas de aluno. |
| fitness_progress_records | Registros de progresso de uma meta. |

### ranking_cycles, ranking_entries, medals e person_medals

| Tabela | Descrição |
|---|---|
| ranking_cycles | Ciclos de ranking, por exemplo mensal. |
| ranking_entries | Pontuação e streak dos alunos. |
| medals | Medalhas configuráveis. |
| person_medals | Medalhas conquistadas pelos alunos. |

### notifications e audit_logs

| Tabela | Descrição |
|---|---|
| notifications | Notificações internas. |
| audit_logs | Auditoria de ações críticas. |

---

## 11. API REST

Padrões gerais:

- Base URL: `/api/v1`
- Autenticação: `Authorization: Bearer {token}`
- Tenant: derivado do token e/ou header controlado `X-Organization-Id` para usuários multi-organização.
- Paginação: `page`, `pageSize`.
- Respostas de erro seguem Problem Details.

### Autenticação

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/auth/login` | email, senha | accessToken, refreshToken, usuário, perfis | 200, 400, 401, 403 |
| POST | `/auth/refresh` | refreshToken | novo accessToken e refreshToken | 200, 400, 401 |
| POST | `/auth/logout` | refreshToken | vazio | 204, 401 |
| POST | `/auth/forgot-password` | email | confirmação | 202, 400 |
| POST | `/auth/reset-password` | token, novaSenha | confirmação | 204, 400 |
| POST | `/auth/change-password` | senhaAtual, novaSenha | confirmação | 204, 400, 401 |
| GET | `/auth/me` | - | dados do usuário autenticado | 200, 401 |

### Organizações e unidades

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/organizations` | dados da organização | organização criada | 201, 400, 409 |
| GET | `/organizations/{id}` | - | organização | 200, 404 |
| PUT | `/organizations/{id}` | dados atualizados | organização | 200, 400, 404 |
| GET | `/organizations/{id}/settings` | - | configurações | 200, 404 |
| PUT | `/organizations/{id}/settings` | configurações | configurações | 200, 400 |
| POST | `/branches` | unidade | unidade criada | 201, 400 |
| GET | `/branches` | filtros | lista paginada | 200 |
| PUT | `/branches/{id}` | unidade | unidade | 200, 400, 404 |
| DELETE | `/branches/{id}` | - | vazio | 204, 404 |

### Pessoas, usuários e papéis

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/persons` | dados pessoais | pessoa criada | 201, 400, 409 |
| GET | `/persons` | filtros | lista paginada | 200 |
| GET | `/persons/{id}` | - | pessoa | 200, 404 |
| PUT | `/persons/{id}` | dados | pessoa | 200, 400, 404 |
| DELETE | `/persons/{id}` | - | vazio | 204, 404 |
| POST | `/persons/{id}/roles` | roleCode | papel vinculado | 201, 400, 404 |
| DELETE | `/persons/{id}/roles/{roleCode}` | - | vazio | 204, 404 |
| POST | `/persons/{id}/user-account` | email, senha inicial | conta criada | 201, 400, 409 |
| PATCH | `/persons/{id}/status` | status | pessoa atualizada | 200, 400, 404 |

### Modalidades

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/modalities` | nome, tipo, descrição | modalidade | 201, 400, 409 |
| GET | `/modalities` | filtros | lista | 200 |
| GET | `/modalities/{id}` | - | modalidade | 200, 404 |
| PUT | `/modalities/{id}` | dados | modalidade | 200, 400, 404 |
| DELETE | `/modalities/{id}` | - | vazio | 204, 404 |
| POST | `/persons/{id}/modalities` | modalityId | vínculo | 201, 400, 404 |
| DELETE | `/persons/{id}/modalities/{modalityId}` | - | vazio | 204, 404 |

### Turmas, horários e matrículas

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/class-groups` | turma | turma criada | 201, 400 |
| GET | `/class-groups` | filtros | lista paginada | 200 |
| GET | `/class-groups/{id}` | - | turma | 200, 404 |
| PUT | `/class-groups/{id}` | dados | turma | 200, 400, 404 |
| DELETE | `/class-groups/{id}` | - | vazio | 204, 404 |
| POST | `/class-groups/{id}/schedules` | dia, início, fim | horário | 201, 400 |
| DELETE | `/class-groups/{id}/schedules/{scheduleId}` | - | vazio | 204, 404 |
| POST | `/class-groups/{id}/enrollments` | personId | matrícula | 201, 400, 409 |
| DELETE | `/class-groups/{id}/enrollments/{enrollmentId}` | - | vazio | 204, 404 |

### Aulas e presenças

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/lessons/generate` | turma, período | aulas geradas | 201, 400 |
| POST | `/lessons` | aula avulsa | aula | 201, 400 |
| GET | `/lessons` | filtros | lista | 200 |
| GET | `/lessons/{id}` | - | aula | 200, 404 |
| PUT | `/lessons/{id}` | dados | aula | 200, 400, 404 |
| PATCH | `/lessons/{id}/cancel` | motivo | aula cancelada | 200, 400, 404 |
| PATCH | `/lessons/{id}/reschedule` | data, horário | aula reagendada | 200, 400, 404 |
| GET | `/lessons/{id}/attendances` | - | chamada | 200, 404 |
| PUT | `/lessons/{id}/attendances` | lista de presenças | chamada salva | 200, 400 |
| POST | `/lessons/{id}/check-in` | personId | presença registrada | 201, 400, 404 |

### Financeiro

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/plans` | plano | plano criado | 201, 400 |
| GET | `/plans` | filtros | lista | 200 |
| PUT | `/plans/{id}` | plano | plano | 200, 400, 404 |
| DELETE | `/plans/{id}` | - | vazio | 204, 404 |
| POST | `/students/{personId}/subscriptions` | planId, início | assinatura | 201, 400 |
| PATCH | `/subscriptions/{id}/cancel` | data | assinatura cancelada | 200, 400 |
| POST | `/invoices/generate` | período | mensalidades | 201, 400 |
| GET | `/invoices` | filtros | lista | 200 |
| GET | `/invoices/{id}` | - | mensalidade | 200, 404 |
| PATCH | `/invoices/{id}/discount` | valor, motivo | mensalidade | 200, 400 |
| POST | `/invoices/{id}/payments` | pagamento | pagamento criado | 201, 400 |
| GET | `/payments` | filtros | lista | 200 |

### Graduação marcial

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/graduation-systems` | sistema | sistema criado | 201, 400 |
| GET | `/graduation-systems` | filtros | lista | 200 |
| POST | `/graduation-systems/{id}/levels` | faixa/grau | nível criado | 201, 400 |
| PUT | `/graduation-levels/{id}` | dados | nível | 200, 400, 404 |
| GET | `/students/{personId}/graduations` | - | graduações | 200 |
| POST | `/students/{personId}/graduations` | modalityId, levelId | graduação inicial | 201, 400 |
| POST | `/students/{personId}/graduation-events` | nova graduação | evento | 201, 400 |
| GET | `/graduation/eligible-students` | modalidade | lista elegível | 200 |

### Evolução física

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/fitness-goals` | meta | meta criada | 201, 400 |
| GET | `/fitness-goals` | filtros | lista | 200 |
| GET | `/fitness-goals/{id}` | - | meta | 200, 404 |
| PUT | `/fitness-goals/{id}` | dados | meta | 200, 400, 404 |
| PATCH | `/fitness-goals/{id}/cancel` | motivo | meta cancelada | 200, 400 |
| POST | `/fitness-goals/{id}/progress` | valor, nota | progresso | 201, 400 |
| GET | `/students/{personId}/fitness-summary` | - | resumo | 200 |

### Gamificação

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| POST | `/ranking-cycles` | ciclo | ciclo criado | 201, 400 |
| GET | `/rankings/current` | filtros | ranking atual | 200 |
| GET | `/rankings/history` | filtros | rankings anteriores | 200 |
| POST | `/medals` | medalha | medalha criada | 201, 400 |
| GET | `/medals` | filtros | lista | 200 |
| GET | `/students/{personId}/medals` | - | medalhas do aluno | 200 |

### Relatórios

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| GET | `/reports/dashboard` | período | KPIs | 200 |
| GET | `/reports/attendance` | filtros | relatório | 200 |
| GET | `/reports/financial` | filtros | relatório | 200 |
| GET | `/reports/student-performance` | filtros | relatório | 200 |
| GET | `/reports/graduation` | filtros | relatório | 200 |

### Notificações e auditoria

| Método | Endpoint | Entrada | Saída | Códigos HTTP |
|---|---|---|---|---|
| GET | `/notifications` | filtros | lista | 200 |
| PATCH | `/notifications/{id}/read` | - | vazio | 204 |
| GET | `/audit-logs` | filtros | lista paginada | 200, 403 |

---

## 12. Segurança

### JWT

- Access token com validade curta, recomendação inicial de 15 minutos.
- Claims mínimas: `sub`, `person_id`, `organization_id`, `roles`, `permissions`, `jti`, `iat`, `exp`.
- Assinatura com chave forte via HMAC SHA-256 ou assimétrica RS256 em fase futura.
- Renovação apenas via refresh token válido.

### Refresh Token

- Refresh token opaco, aleatório e armazenado apenas com hash no banco.
- Rotação a cada uso.
- Revogação em logout.
- Detecção de reuso de refresh token e revogação da cadeia.

### OAuth2

- No MVP, autenticação própria com JWT.
- Preparar arquitetura para login externo futuro usando OAuth2/OpenID Connect, como Google ou Microsoft, sem acoplar domínio à implementação.

### RBAC

Papéis iniciais:

- **Owner:** controle total da organização.
- **Admin:** gestão operacional completa.
- **Finance:** financeiro e relatórios financeiros.
- **Receptionist:** cadastro, presença e consultas limitadas.
- **Teacher:** turmas, aulas, presença, evolução e graduação autorizada.
- **Student:** consulta própria no app.

Regras:

- Permissões sempre avaliadas por organização.
- Usuário com múltiplos perfis recebe união controlada de permissões.
- A API deve validar recurso, tenant e permissão.

### Criptografia

- HTTPS obrigatório.
- Hash de senha com algoritmo forte.
- Dados sensíveis, como observações médicas, devem ter acesso restrito.
- Backups devem ser protegidos por criptografia ou armazenamento seguro.

### Rate Limiting

- Login: limite por IP e por e-mail.
- Refresh token: limite por usuário/IP.
- Endpoints públicos: limites agressivos.
- Endpoints autenticados: limites por usuário e organização.
- Redis pode ser usado para rate limiting distribuído.

### Proteção OWASP

- Validação de entrada e saída.
- Parametrização de SQL com Dapper para evitar SQL Injection.
- CORS restrito por ambiente.
- Headers de segurança.
- Proteção contra brute force.
- Logs sem senhas, tokens ou dados sensíveis.
- Controle de acesso server-side obrigatório.
- Sanitização de campos textuais exibidos no frontend.
- Uploads futuros com validação de tipo, tamanho e antivírus se necessário.

---

## 13. Logs

### Estratégia

- Logs estruturados em JSON.
- Correlação por `correlation_id` em todas as requisições.
- Inclusão de `organization_id`, `person_id`, `endpoint`, `method`, `status_code`, `elapsed_ms` quando aplicável.
- Separação de logs de aplicação e auditoria.
- Não registrar senha, tokens, documentos completos ou observações médicas em logs comuns.

### Níveis

- **Trace/Debug:** somente local/desenvolvimento.
- **Information:** eventos normais de operação.
- **Warning:** comportamento inesperado recuperável.
- **Error:** falhas de aplicação.
- **Critical:** indisponibilidade, corrupção, falha grave de segurança.

### Eventos obrigatórios

- Login, logout e falha de login.
- Refresh token inválido/reutilizado.
- Criação/alteração/exclusão de aluno.
- Alterações financeiras.
- Alterações de graduação.
- Mudança de permissões.
- Cancelamento/reagendamento de aula.
- Erros 5xx.
- Jobs de geração de mensalidades e aulas.

### Retenção

- Logs operacionais: 7 a 15 dias no MVP.
- Auditoria: mínimo 1 ano ou conforme política da organização.
- Exportação/arquivamento futuro para storage barato.

---

## 14. Monitoramento

Usar apenas ferramentas gratuitas e open source:

- OpenTelemetry Collector.
- Prometheus.
- Grafana OSS.
- Node Exporter.
- cAdvisor, se usar Docker.
- Loki/Promtail opcional para logs centralizados, se a VPS suportar.

### Métricas

#### API

- Requests por segundo.
- Latência p50, p95, p99.
- Taxa de erro 4xx/5xx.
- Tempo por endpoint.
- Uso de CPU/memória.
- Tamanho de resposta.

#### Banco

- Conexões ativas.
- Locks.
- Queries lentas.
- Tamanho do banco.
- Uso de disco.
- Taxa de cache hit.

#### Redis

- Memória usada.
- Hit/miss rate.
- Conexões.
- Evictions.

#### Negócio

- Alunos ativos.
- Alunos inadimplentes.
- Presença média.
- Mensalidades vencidas.
- Check-ins por dia.
- Novas matrículas.
- Cancelamentos.

### Dashboards

- **Dashboard API:** latência, erros, throughput.
- **Dashboard Infra:** CPU, memória, disco, rede.
- **Dashboard Banco:** conexões, queries lentas, locks, tamanho.
- **Dashboard Financeiro:** receita prevista, recebida, vencida.
- **Dashboard Presença:** frequência por turma/modalidade.
- **Dashboard Jobs:** execuções, duração e falhas.

### Alertas

- API com erro 5xx acima de 5% por 5 minutos.
- Latência p95 acima de 1 segundo por 10 minutos.
- CPU acima de 85% por 10 minutos.
- Memória acima de 85%.
- Disco acima de 80%.
- Banco indisponível.
- Falha em backup.
- Job financeiro falhando.
- Aumento anormal de falhas de login.

---

## 15. Testes

### Unitários

- Testar entidades, value objects, regras de graduação, cálculo de frequência, pontuação e mensalidades.
- Framework: xUnit ou NUnit.
- Mocks para interfaces externas.
- Cobertura mínima recomendada: 70% em domínio e aplicação.

### Integração

- Testar repositórios Dapper com PostgreSQL real via Testcontainers.
- Testar API com WebApplicationFactory.
- Validar migrations.
- Validar transações financeiras.
- Validar isolamento por tenant.

### E2E

- Web: Playwright ou ferramenta equivalente.
- Mobile: testes Flutter integration_test.
- Fluxos críticos:
  - Login.
  - Cadastro de aluno.
  - Criação de turma.
  - Registro de presença.
  - Registro de pagamento.
  - Graduação.

### Performance

- k6 para carga de endpoints críticos.
- Cenários:
  - Login simultâneo.
  - Listagem de alunos paginada.
  - Registro de presença em turma grande.
  - Dashboard administrativo.
  - Geração de mensalidades.

### Segurança

- Testes de autorização por papel.
- Testes de tentativa de acesso cross-tenant.
- Testes de rate limiting.
- Testes de SQL injection em filtros.

---

## 16. DevOps

### Docker

Necessário desde o início para padronizar ambientes.

Containers:

- API.
- Web Blazor.
- Worker.
- PostgreSQL local.
- Redis.
- Prometheus.
- Grafana.
- OpenTelemetry Collector.

### Docker Compose

Usar para:

- Desenvolvimento local.
- Homologação simples.
- Produção inicial na VPS, preferencialmente orquestrada pelo Coolify.

### Kubernetes

Não recomendado no MVP.

Justificativa:

- Aumenta custo operacional.
- VPS inicial não exige complexidade de cluster.
- Docker Compose/Coolify atende ao estágio inicial.

Kubernetes pode ser considerado quando houver:

- Necessidade de autoscaling.
- Múltiplas instâncias da API.
- Alta disponibilidade multi-node.
- Equipe DevOps dedicada.

### GitHub Actions

Responsável por:

- Build.
- Testes.
- Análise estática.
- Build de imagem Docker.
- Push para registry.
- Deploy via Coolify webhook ou SSH seguro.

---

## 17. Deploy

### Desenvolvimento

```text
Developer Machine
├── Docker Compose
│   ├── PostgreSQL
│   ├── Redis
│   ├── API
│   ├── Web
│   ├── Worker
│   ├── Prometheus
│   └── Grafana
└── Flutter local/emulador
```

Características:

- Banco com seeds.
- Logs verbosos.
- Swagger habilitado.
- Hot reload quando possível.

### Homologação

```text
Hostinger VPS / Coolify
├── kiai-api-staging
├── kiai-web-staging
├── kiai-worker-staging
├── postgres-staging
├── redis-staging
└── observability-staging
```

Características:

- Base isolada.
- Dados sintéticos ou anonimizados.
- URL própria.
- Deploy automático a partir da branch `develop`.

### Produção

```text
Hostinger VPS / Coolify
├── Reverse Proxy HTTPS
├── kiai-api-prod
├── kiai-web-prod
├── kiai-worker-prod
├── postgres-prod
├── redis-prod
├── prometheus-prod
├── grafana-prod
└── backup jobs
```

Características:

- HTTPS obrigatório.
- Variáveis de ambiente seguras.
- Backup diário.
- Observabilidade ativa.
- Deploy a partir de tags ou branch `main`.
- Rollback por imagem anterior.

---

## 18. Repositórios

### Decisão

Usar **Monorepo no GitHub** inicialmente.

Nome sugerido:

```text
kiai-control
```

### Justificativa

- Facilita alinhamento entre backend, frontend, mobile, infra e documentação no início.
- Simplifica versionamento do produto.
- Facilita refatorações coordenadas.
- Reduz overhead administrativo.
- Adequado para equipe pequena.

### Estrutura lógica interna

```text
backend/
frontend/
mobile/
infra/
database/
docs/
```

### Possível evolução para multirepo

Separar futuramente se houver equipes independentes:

```text
kiai-control-api
kiai-control-admin
kiai-control-app
kiai-control-infra
kiai-control-database
kiai-control-docs
```

Critérios para separar:

- Times diferentes.
- Ciclos de release independentes.
- Escala organizacional.
- Necessidade de permissões separadas por código.

---

## 19. CI/CD

### Pipeline backend

1. Checkout.
2. Setup .NET 10 SDK.
3. Restore.
4. Build.
5. Testes unitários.
6. Testes de integração com PostgreSQL/Redis via containers.
7. Análise estática.
8. Publicação de artefato.
9. Build da imagem Docker.
10. Push para registry.
11. Deploy em homologação ou produção.

### Pipeline frontend Blazor

1. Checkout.
2. Setup .NET 10 SDK.
3. Restore.
4. Build.
5. Testes.
6. Build da imagem Docker.
7. Deploy.

### Pipeline mobile Flutter

1. Checkout.
2. Setup Flutter.
3. Flutter pub get.
4. Analyze.
5. Test.
6. Build APK/AAB em branches/tags específicas.
7. Publicação manual ou automatizada em fase futura.

### Estratégia de branches

- `main`: produção.
- `develop`: homologação.
- `feature/*`: desenvolvimento.
- `hotfix/*`: correções urgentes.
- tags `vX.Y.Z`: releases.

### Gates obrigatórios

- Pull request obrigatório para `develop` e `main`.
- Testes passando.
- Revisão de código.
- Sem segredos no repositório.
- Migrations revisadas.

---

## 20. Documentação

Documentos obrigatórios:

- `README.md`: visão geral, setup local, comandos úteis.
- `SYSTEM_DESIGN.md`: arquitetura e decisões técnicas.
- `docs/api/openapi.md`: documentação da API e exemplos.
- `docs/api/auth.md`: autenticação, tokens e permissões.
- `docs/architecture/clean-architecture.md`: camadas e dependências.
- `docs/architecture/ddd.md`: bounded contexts, agregados e regras.
- `docs/deploy/development.md`: ambiente local.
- `docs/deploy/staging.md`: deploy de homologação.
- `docs/deploy/production.md`: deploy prod.
- `docs/operations/backup-restore.md`: backup e restauração.
- `docs/operations/observability.md`: métricas, logs, traces, dashboards e alertas.
- `docs/operations/incidents.md`: runbook de incidentes.
- `docs/decisions/ADR-0001-monorepo.md`: decisão de monorepo.
- `docs/decisions/ADR-0002-dapper.md`: decisão por Dapper.
- `docs/decisions/ADR-0003-deploy-coolify.md`: decisão de deploy inicial.

---

## 21. Roadmap

### Fase 1 - MVP operacional

Objetivo: permitir que uma academia opere cadastro, turmas, presença e financeiro básico.

Entregas:

- Autenticação JWT.
- Organizações e usuários.
- Perfis múltiplos.
- Alunos e professores.
- Modalidades.
- Turmas e horários.
- Matrícula.
- Aulas.
- Presença.
- Planos, mensalidades e pagamentos manuais.
- Dashboard básico.
- App mobile com login, agenda, presença e consulta de dados.
- Observabilidade básica.
- Deploy produção inicial.

### Fase 2 - Evolução e engajamento

Objetivo: diferenciar o produto para artes marciais e atividades físicas.

Entregas:

- Graduação completa por modalidade.
- Elegibilidade de graduação.
- Metas físicas.
- Progresso do aluno.
- Ranking de presença.
- Medalhas.
- Relatórios avançados.
- Notificações internas.
- Exportações CSV/Excel.

### Fase 3 - Automação e integrações

Objetivo: aumentar automação, retenção e monetização.

Entregas:

- Integração Pix/cobrança.
- Integração WhatsApp/e-mail transacional.
- Importação por planilha.
- Alertas de risco de evasão/inadimplência.
- Contratos e documentos.
- Multi-unidade avançado.
- Integração com catracas/biometria se necessário.
- Recursos de IA, se houver maturidade de dados.

---

## 22. Estimativa

### Tempo de desenvolvimento

Considerando uma equipe pequena e escopo MVP:

- **Fase 1:** 12 a 16 semanas.
- **Fase 2:** 8 a 12 semanas adicionais.
- **Fase 3:** 12 a 20 semanas adicionais, dependendo de integrações.

### Equipe necessária

Equipe mínima recomendada:

- 1 arquiteto/back-end sênior .NET.
- 1 desenvolvedor back-end .NET.
- 1 desenvolvedor Blazor.
- 1 desenvolvedor Flutter.
- 1 QA funcional/automatizador parcial.
- 1 PO/analista de produto.
- DevOps pode ser acumulado pelo arquiteto no MVP.

Equipe enxuta possível:

- 1 fullstack .NET/Blazor.
- 1 Flutter.
- 1 PO/QA parcial.

Nesse cenário, o prazo tende a aumentar.

### Complexidade

- **Complexidade geral:** média-alta.
- **Motivos:** multi-tenant, financeiro, permissões, graduação customizável, gamificação, mobile e observabilidade.
- **Maior risco funcional:** modelagem correta de graduação por modalidade.
- **Maior risco técnico:** isolamento por tenant e segurança financeira.

---

## 23. Riscos

| Risco | Impacto | Probabilidade | Mitigação |
|---|---:|---:|---|
| Escopo crescer demais no MVP | Alto | Alta | Roadmap por fases e backlog priorizado. |
| Regras de graduação variarem muito por arte marcial | Alto | Alta | Modelagem flexível de sistemas, níveis e critérios. |
| Falha de isolamento multi-tenant | Crítico | Média | Testes automatizados cross-tenant e filtro obrigatório. |
| Inconsistência financeira | Alto | Média | Transações, auditoria, testes de integração. |
| Consultas lentas em dashboards | Médio | Média | Índices, views/materialização futura e cache. |
| VPS insuficiente | Médio | Média | Monitoramento, otimização e plano de upgrade. |
| Falha de backup | Crítico | Baixa/Média | Backup automatizado e teste de restore. |
| Tokens comprometidos | Alto | Média | Refresh token rotativo, expiração curta, revogação. |
| Dependência excessiva de Coolify/VPS | Médio | Média | Docker padronizado e documentação de migração. |
| Mobile atrasar o MVP | Médio | Média | Priorizar funcionalidades essenciais no app. |
| Falta de UX adequada para professores | Médio | Média | Validar fluxo de chamada com usuários reais. |
| Dados sensíveis expostos em logs | Alto | Baixa/Média | Política de logging e filtros de mascaramento. |

---

## 24. Próximos Passos

### Backlog inicial do projeto

#### Épico 1 - Fundação técnica

- Criar monorepo.
- Criar solução .NET.
- Configurar projetos KiaiControl.Api, KiaiControl.Core, KiaiControl.Contracts, KiaiControl.UseCases, KiaiControl.Repositories e KiaiControl.Services.
- Configurar Blazor Server com MudBlazor.
- Criar projeto Flutter.
- Criar Docker Compose local.
- Configurar PostgreSQL, Redis, Prometheus e Grafana local.
- Configurar GitHub Actions inicial.
- Configurar padrões de build, lint e testes.

#### Épico 2 - Identidade e segurança

- Implementar cadastro de organização.
- Implementar pessoas e contas de usuário.
- Implementar login JWT.
- Implementar refresh token rotativo.
- Implementar RBAC.
- Implementar middleware de tenant.
- Implementar auditoria básica.
- Implementar rate limiting.

#### Épico 3 - Cadastros operacionais

- Implementar modalidades.
- Implementar alunos.
- Implementar professores.
- Implementar perfis múltiplos.
- Implementar múltiplas modalidades por pessoa.
- Implementar unidades/filiais.

#### Épico 4 - Turmas e aulas

- Implementar turmas.
- Implementar horários recorrentes.
- Implementar geração de aulas.
- Implementar matrícula em turma.
- Implementar cancelamento e reagendamento.

#### Épico 5 - Presença

- Implementar lista de chamada.
- Implementar check-in por professor.
- Implementar falta justificada.
- Implementar cálculo de frequência.
- Implementar atualização de streak.

#### Épico 6 - Financeiro básico

- Implementar planos.
- Implementar assinatura do aluno.
- Implementar geração de mensalidades.
- Implementar registro de pagamento manual.
- Implementar inadimplência.
- Implementar relatório financeiro básico.

#### Épico 7 - Graduação marcial

- Implementar sistema de graduação.
- Implementar faixas/graus.
- Implementar graduação atual.
- Implementar histórico de promoção.
- Implementar critérios mínimos.
- Implementar sugestão de elegibilidade.

#### Épico 8 - Evolução física

- Implementar metas.
- Implementar progresso.
- Implementar categorias.
- Implementar pontuação por meta.
- Implementar resumo de evolução.

#### Épico 9 - Gamificação

- Implementar ranking mensal.
- Implementar pontuação por presença.
- Implementar medalhas.
- Implementar privacidade no ranking.

#### Épico 10 - Dashboards e relatórios

- Implementar dashboard administrativo.
- Implementar relatório de presença.
- Implementar relatório financeiro.
- Implementar relatório de evolução.
- Implementar filtros por período, turma, modalidade e aluno.

#### Épico 11 - Mobile

- Implementar login.
- Implementar home do aluno.
- Implementar agenda.
- Implementar presença/frequência.
- Implementar pagamentos.
- Implementar graduação/evolução.
- Implementar ranking e medalhas.
- Implementar visão professor para chamada.

#### Épico 12 - Operação e produção

- Configurar Coolify.
- Configurar domínios e HTTPS.
- Configurar deploy homologação.
- Configurar deploy produção.
- Configurar backup.
- Configurar dashboards Grafana.
- Configurar alertas.
- Documentar operação.

---

## Observações finais de arquitetura

O Kiai Control deve usar o monorepo apenas como acelerador inicial de contexto e padronização. A estrutura proposta já considera a extração futura em repositórios independentes, evitando acoplamento indevido desde o primeiro commit. A regra principal é: cada projeto deve ter README, pipeline, comandos de teste e fronteiras claras.
## Referências consultadas

- BJJ Control como referência funcional de mercado: https://bjjcontrol.com.br/
- Política oficial de suporte do .NET: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- Política oficial de versionamento do PostgreSQL: https://www.postgresql.org/support/versioning/
- Documentação de OpenTelemetry com Grafana: https://grafana.com/docs/opentelemetry/
