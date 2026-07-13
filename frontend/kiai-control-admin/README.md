# Kiai Control Admin

## Objetivo
Painel administrativo web do Kiai Control para operacao da academia, com foco em fluxos de gestao e acompanhamento.

## Responsabilidades
- Entregar a interface administrativa do produto.
- Consumir a API backend via contratos versionados.
- Aplicar autenticacao/autorizacao e controles por tenant no frontend.

## Stack
- .NET 10
- Blazor Server
- MudBlazor

## Como executar localmente
```bash
dotnet restore frontend/kiai-control-admin/KiaiControl.Admin.slnx
dotnet run --project frontend/kiai-control-admin/src/KiaiControl.Admin/KiaiControl.Admin.csproj
```

## Como testar
```bash
dotnet test frontend/kiai-control-admin/KiaiControl.Admin.slnx
```

## Variaveis de ambiente
- ASPNETCORE_ENVIRONMENT
- ASPNETCORE_URLS

## Estrutura interna
- src/KiaiControl.Admin: aplicacao Blazor Server.
- tests: projetos de teste do frontend admin.

## Padroes de codigo
- Seguir SYSTEM_DESIGN.md e COPILOT.md.
- Separar paginas, componentes e clientes por contexto de negocio.
- Evitar acoplamento com implementacoes internas do backend.

## CI/CD
Pipeline dedicada de frontend-admin deve compilar, testar e publicar artefatos.

## Observabilidade
Logs estruturados no servidor Blazor e correlacao com traces da API quando aplicavel.

## Decisoes importantes
- Monorepo inicial com separacao por pasta de futuro repositorio.
