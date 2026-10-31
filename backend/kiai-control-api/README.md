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

Permissoes efetivas combinam os defaults de `role_permissions` com overrides por organizacao em `organization_role_permissions`; elas sao adicionadas ao access JWT e exigidas por policies da API. Aplique as migrations `001`-`007` em ordem antes de usar a API; `004` e `005` preparam as permissoes, `006_open_modality_schedules.sql` adiciona horarios recorrentes por modalidade e `007_remove_class_groups.sql` remove por completo o modelo de turmas e matriculas fixas. O script `database/kiai-control-database/scripts/create_database.sh` registra as migrations na tabela `schema_migrations` e aplica apenas as pendentes.

No fluxo operacional, o aluno e vinculado uma vez a modalidade e pode participar de qualquer sessao dela. Horarios recorrentes sao cadastrados em `/modalities/{id}/schedules`, com unidade e professor opcionais; a geracao e idempotente para o mesmo horario/data. A chamada usa alunos ativos vinculados a modalidade. A migration `007` converte aulas e ciclos de ranking para sua modalidade, transfere matriculas ativas para `person_modalities` e remove as tabelas, campos e permissoes de turma fixa; esses registros nao permanecem no schema nem na API.

Login, refresh, logout, change-password e recuperacao/reset estao limitados a 10 requisicoes por IP por minuto. A recuperacao retorna mensagem generica e nao expoe o token; a entrega do link depende de um provedor de e-mail ainda nao configurado.

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
- O estado de conclusao e as lacunas verificadas estao em [PROJECT_IMPLEMENTATION_PLAN.md](../../PROJECT_IMPLEMENTATION_PLAN.md).
- A existencia de controllers nao comprova integracao com PostgreSQL; os testes de repositorio contra banco real continuam sendo criterio de aceite.
