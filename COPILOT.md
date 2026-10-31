# Kiai Control - Instruções para IA

## Planejamento e estado do projeto

- Consultar [PROJECT_IMPLEMENTATION_PLAN.md](PROJECT_IMPLEMENTATION_PLAN.md) antes de iniciar uma evolucao; ele e a fonte atual de prioridades, lacunas verificadas e criterios de aceite.
- Tratar `SYSTEM_DESIGN.md` como estado desejado e os checklists antigos como historicos, nao como evidencia de implementacao.
- Atualizar o plano somente depois de validar a entrega com os testes/checks definidos; nao marcar como concluido apenas por existir controller, tela ou build.

## Arquitetura

Utilizar obrigatoriamente:

- KiaiControl.Api
- KiaiControl.Core
- KiaiControl.Contracts
- KiaiControl.UseCases
- KiaiControl.Repositories
- KiaiControl.Services

## Regras de Código

- .NET 10
- ASP.NET Core
- Dapper
- PostgreSQL
- Redis
- JWT Authentication

## Controllers

Usar controllers por contexto:

- StudentsController
- TeachersController
- BillingController
- AttendanceController

Não criar controllers por operação.

## Contratos

Preferir:

- Request
- Response
- Entity

Evitar DTOs genéricos desnecessários.

## Repositórios

- Utilizar Dapper
- SQL explícito
- Sem Entity Framework

## Entidades

Representam o domínio e refletem o banco de forma controlada.

## Organização dos UseCases

Organizar por contexto:

Students/
Attendance/
Billing/
Graduation/
Fitness/
Reports/

## Testes

Utilizar:

- xUnit
- Integration Tests
- Architecture Tests
- Performance Tests

## Multi-Tenant

Toda consulta deve respeitar organization_id.

## Segurança

- JWT
- Refresh Token
- RBAC
- Tenant Isolation

## Observabilidade

- OpenTelemetry
- Prometheus
- Grafana

Sempre seguir as definições do SYSTEM_DESIGN.md.