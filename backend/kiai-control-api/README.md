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
- Os endpoints de contexto criados aqui sao apenas smoke endpoints para validar o bootstrap da API.
