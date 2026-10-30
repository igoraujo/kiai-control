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

Os testes de integracao PostgreSQL usam Testcontainers e exigem um daemon Docker acessivel. Nesta maquina, sem Docker ou PostgreSQL local, execute os testes unitarios sem tentar abrir conexoes:

```bash
dotnet test backend/kiai-control-api/tests/KiaiControl.UnitTests/KiaiControl.UnitTests.csproj
```

O estado da validacao contra banco real esta registrado em [PROJECT_IMPLEMENTATION_PLAN.md](../../PROJECT_IMPLEMENTATION_PLAN.md); compilar o harness nao conta como teste de conexao aprovado.

## Variaveis e configuracao
- `ConnectionStrings:PostgreSql`
- `ConnectionStrings:Redis`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`

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
