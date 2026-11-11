# KiaiControl API

## Objetivo
Base da API do Kiai Control para web e mobile, organizada como monolito modular com camadas separadas para API, Core, Contracts, UseCases, Repositories e Services.

## Responsabilidades
- Expor endpoints HTTP em `/api/v1`.
- Centralizar autenticacao, autorizacao, tenant context e observabilidade.
- Orquestrar casos de uso e integracoes de infraestrutura.

## Stack
- .NET 10
- ASP.NET Core Web API
- Dapper
- PostgreSQL
- Redis
- JWT Authentication

## Como executar localmente

```bash
dotnet restore backend/kiai-control-api/KiaiControl.Api.slnx
dotnet run --project backend/kiai-control-api/src/KiaiControl.Api/KiaiControl.Api.csproj
```

## Como testar

```bash
dotnet build backend/kiai-control-api/KiaiControl.Api.slnx
dotnet test backend/kiai-control-api/KiaiControl.Api.slnx
```

Os testes de integracao PostgreSQL usam Testcontainers e exigem um daemon Docker acessivel. Para executar a suite completa, incluindo migrations e integracao real:

```bash
dotnet test backend/kiai-control-api/KiaiControl.Api.slnx
```

Para executar somente os testes unitarios, sem iniciar PostgreSQL:

```bash
dotnet test backend/kiai-control-api/tests/KiaiControl.UnitTests/KiaiControl.UnitTests.csproj
```

O workflow de CI executa separadamente as suites unitarias, de integracao, arquitetura e Admin. O estado da validacao contra banco real esta registrado em [PROJECT_IMPLEMENTATION_PLAN.md](../../PROJECT_IMPLEMENTATION_PLAN.md); compilar o harness nao conta como teste de conexao aprovado.

## Variaveis e configuracao
- `ConnectionStrings:PostgreSql`
- `ConnectionStrings:Redis`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey` (obrigatoria fora de Development, minimo de 32 bytes para HS256)

Permissoes efetivas combinam os defaults de `role_permissions` com overrides por organizacao em `organization_role_permissions`; elas sao adicionadas ao access JWT e exigidas por policies da API. Aplique as migrations `001`-`014` em ordem antes de usar a API; `004` e `005` preparam as permissoes, `006_open_modality_schedules.sql` adiciona horarios recorrentes por modalidade, `007_remove_class_groups.sql` remove o modelo de turmas, `008_graduation_belts_and_degrees.sql` organiza a graduacao em faixas e graus, `009_remove_non_martial_graduation_data.sql` remove dados de graduacao associados a modalidades que nao sao artes marciais, e `010_platform_owner_and_temporary_passwords.sql` adiciona as flags de dono global e troca de senha temporaria. O script `database/kiai-control-database/scripts/create_database.sh` registra as migrations na tabela `schema_migrations` e aplica apenas as pendentes.

No fluxo operacional, o aluno e vinculado uma vez a modalidade e pode participar de qualquer sessao dela. Horarios recorrentes sao cadastrados em `/modalities/{id}/schedules`, com unidade e professor opcionais; a geracao e idempotente para o mesmo horario/data. A chamada usa alunos ativos vinculados a modalidade. A migration `007` converte aulas e ciclos de ranking para sua modalidade, transfere matriculas ativas para `person_modalities` e remove as tabelas, campos e permissoes de turma fixa; esses registros nao permanecem no schema nem na API.

Login, refresh, logout, change-password e recuperacao/reset estao limitados por padrao a 10 requisicoes por IP por minuto (`RateLimiting:AuthPermitLimit`). A recuperacao retorna mensagem generica e nao expoe o token; a entrega do link usa SMTP com TLS quando configurado (`Smtp:Host`, `Port`, `Username`, `Password`, `From`, `ResetUrl` HTTPS). Sem provedor configurado, a academia pode redefinir uma senha temporaria na gestao de pessoas.

Novas senhas devem ter no mínimo 6 caracteres, incluindo ao menos uma letra maiúscula, uma letra minúscula e um caractere especial. `Contracts/Auth/PasswordPolicy.cs` centraliza essa regra para cadastro da academia, bootstrap da plataforma, criação/redefinição de acesso, troca e recuperação de senha. O painel compartilha o mesmo validador. A validação considera letras e símbolos Unicode; espaços não contam como caracteres especiais.

Excecoes nao tratadas retornam Problem Details generico, sem stack trace ou detalhes internos da API. O `X-Correlation-Id` da resposta pode ser usado para localizar os detalhes nos logs do servidor.

## Estrutura interna
- `src/KiaiControl.Api`: composicao HTTP, middlewares e controllers.
- `src/KiaiControl.Core`: entidades, contratos internos e contexto de tenant.
- `src/KiaiControl.Contracts`: requests e responses publicas.
- `src/KiaiControl.UseCases`: orquestracao da aplicacao.
- `src/KiaiControl.Repositories`: acesso a dados com Dapper.
- `src/KiaiControl.Services`: integracoes tecnicas e servicos transversais.

## Observacoes
- Toda consulta multi-tenant deve respeitar `organization_id`.
- As classes em `Repositories/Queries` expõem SQL completo em `const string`, formatado como `BranchQueries`. Valores são vinculados por parâmetros nomeados do Dapper. Não compor SQL com concatenação, interpolação, `Replace` ou `Split`. Consultas auxiliares também ficam nas classes de queries.
- O teste arquitetural de convenção impede campos SQL inicializados em runtime e métodos construtores de SQL nessas classes. A integração verifica buscas literais com aspas, `%`, `_`, barras e tentativas de SQL injection, preservando o tenant e os totais.
- O estado de conclusao e as lacunas verificadas estao em [PROJECT_IMPLEMENTATION_PLAN.md](../../PROJECT_IMPLEMENTATION_PLAN.md).
- A existencia de controllers nao comprova integracao com PostgreSQL; os testes de repositorio contra banco real continuam sendo criterio de aceite.

## Admin e portal pessoal

- `GET /api/v1/catalog/{clients|teachers|branches|modalities|lessons}`: paginação com total e busca/filtros no servidor; contratos de listagem anteriores continuam disponíveis.
- Os filtros `from`/`to` de aulas aceitam offsets locais e são convertidos para UTC antes da consulta PostgreSQL, preservando os limites do período informado.
- Os catálogos de alunos, professores, modalidades e aulas aceitam `area=shared|fight|gym` (opcional, padrão Geral). O recorte antecede paginação e contagem; alunos usam os vínculos de `person_modalities`, modalidades híbridas pertencem às duas áreas e o filtro não amplia o escopo do professor nem o tenant. `ClientResponse.Areas` é derivado desses vínculos, sem coluna persistida ou duplicação de cadastro.
- Listagem e opções de metas também aceitam `area`; metas com modalidade seguem seu tipo, enquanto metas sem modalidade seguem os vínculos do aluno. As opções de graduação aceitam o mesmo filtro de alunos. Áreas inválidas retornam 400. Os papéis e permissões existentes permanecem necessários.
- `/api/v1/people`: diretório de equipe, vínculo de perfis e administração tenant-scoped de contas. E-mail de contato e e-mail de login são campos distintos.
- `/api/v1/organizations/settings`: configuração da agenda e check-in.
- `/api/v1/portal/*`: perfil, modalidades, aulas, jornada, metas/progresso, faturas e pagamentos do próprio aluno. Não recebe um identificador arbitrário de aluno para consultar dados pessoais.
- `/api/v1/mobile/teacher/*` e rotas administrativas de aulas/chamadas: aplicam a associação do professor autenticado à aula.

As migrations `011` (versão de sessão), `012` (direção/valor inicial de metas fitness) e `013` (permissões de consulta) são necessárias nesta versão. O JWT inclui a versão de sessão; bloqueio, redefinição de senha, mudança de login e encerramento administrativo de sessões invalidam o acesso anterior. As permissões e papéis do token são comparados ao estado atual no banco. Contas antigas precisam entrar novamente após a atualização.

A migration `014_backfill_organization_settings.sql` cria configurações padrão para academias antigas sem esse registro, sem alterar políticas existentes. A leitura também aceita esses cadastros legados e a gravação cria o registro ausente. Isso corrige a falha da agenda do aluno causada por `QuerySingleAsync` sem resultado. O seed administrativo legado passa a criar as configurações junto da academia.

As permissões `.read` dos quatro cadastros autorizam GET; `.manage` continua necessária para mutações. Concessões incompatíveis com o papel são rejeitadas. Recuperação de senha não expõe token nem existência do e-mail e invalida sessões anteriores após concluir a troca.
