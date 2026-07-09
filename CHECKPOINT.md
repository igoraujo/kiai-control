# Checkpoint de Desenvolvimento - Kiai Control API

## Data
2026-07-09

## Status Geral
A API backend evoluiu de base funcional inicial para uma estrutura com CRUD completo, regras de negocio e persistencia SQL via Dapper nos contextos principais: Clients, Teachers, Attendance e Billing.

## O que ja foi implementado

### Arquitetura e estrutura
- Estrutura de solucao .NET 10 mantida com separacao por projetos: Api, Core, Contracts, UseCases, Repositories e Services.
- Configuracao de API mantida com JWT, Swagger, health checks e middleware de contexto de organizacao.
- Organizacao em camadas preservada, com regras de negocio centralizadas em UseCases e acesso a dados em Repositories.

### Renomeacao de dominio
- Nomenclatura de Students/Student foi refatorada para Clients/Client no backend e no DBML.
- Controllers, contratos, entidades, use cases, repositorios, testes e referencias de codigo foram alinhados para Clients.

### Endpoints implementados (CRUD)
- Clients
  - POST /api/v1/clients
  - GET /api/v1/clients
  - GET /api/v1/clients/{id}
  - PUT /api/v1/clients/{id}
  - DELETE /api/v1/clients/{id}
- Teachers
  - POST /api/v1/teachers
  - GET /api/v1/teachers
  - GET /api/v1/teachers/{id}
  - PUT /api/v1/teachers/{id}
  - DELETE /api/v1/teachers/{id}
- Attendance
  - POST /api/v1/attendance
  - GET /api/v1/attendance
  - GET /api/v1/attendance/{id}
  - PUT /api/v1/attendance/{id}
  - DELETE /api/v1/attendance/{id}
- Billing
  - POST /api/v1/billing
  - GET /api/v1/billing
  - GET /api/v1/billing/{id}
  - PUT /api/v1/billing/{id}
  - DELETE /api/v1/billing/{id}

### Regras de negocio implementadas
- Validacoes de campos obrigatorios em UseCases (ex.: OrganizationId, ClientId, LessonId, ClientPlanSubscriptionId).
- Validacao de status e nome em Clients/Teachers.
- Validacao de valor financeiro (Amount >= 0) em Billing.
- Paginacao padrao em listagens (page, pageSize) com limites defensivos para performance.

### Persistencia e queries SQL
- Repositorios SQL com Dapper implementados para Clients, Teachers, Attendance e Billing.
- Pasta Queries criada e organizada por contexto:
  - Repositories/Queries/Clients/ClientQueries.cs
  - Repositories/Queries/Teachers/TeacherQueries.cs
  - Repositories/Queries/Attendance/AttendanceQueries.cs
  - Repositories/Queries/Billing/BillingQueries.cs
- Decisao adotada: classes estaticas por repository para centralizar SQL explicito, melhorar manutencao e evitar sobrecarga desnecessaria.

### Injeção de dependencia
- Registro de repositorios removido da camada UseCases.
- DI centralizada em Repositories:
  - Usa implementacoes SQL quando connection string esta configurada.
  - Mantem fallback para in-memory quando nao ha configuracao de banco, garantindo bootstrap e testes locais.

### Banco de dados (DBML)
- Modelo DBML normalizado atualizado para nomenclatura Clients.
- Estrutura multi-tenant preservada com organization_id e indices por tenant/contexto.

## Limitações atuais
- Ainda nao foi criada migracao SQL inicial oficial para provisionar todo o schema no PostgreSQL.
- Os testes atuais validam compilacao e fluxo base, mas ainda nao exercitam repositorios SQL contra banco real de teste.
- Contextos adicionais do SYSTEM_DESIGN (Organizations completos, Modalities, ClassGroups/Lessons completos, Graduation/Fitness/Ranking/Notifications/Audit completos) ainda precisam ser implementados end-to-end.

## Como continuar o desenvolvimento

### Proximo passo recomendado
Criar migracoes SQL iniciais do schema (a partir do DBML atual) e ativar testes de integracao reais contra PostgreSQL.

### Ordem sugerida para continuidade
1. Gerar e versionar scripts de migracao SQL para tabelas, enums, indices e constraints.
2. Adicionar testes de integracao com banco de teste para validar queries Dapper por contexto.
3. Implementar contextos faltantes do SYSTEM_DESIGN (Modalities, Classes/Lessons, Graduation, Fitness, Ranking, Notifications, Audit).
4. Evoluir autenticacao/autorizacao para fluxo completo com usuarios, roles, refresh token e isolamento por tenant em todas as operacoes.
5. Expandir cobertura de testes (unitarios de regras de negocio e testes de performance com cenarios reais).

## Verificação feita
A solucao foi validada com:
- dotnet test KiaiControl.Api.slnx

Resultado verificado:
- 5 testes passaram
- 0 falhas

## Observações para outra IA
- Nao reescreva a estrutura base sem necessidade.
- Preserve a arquitetura em camadas e a separacao de responsabilidades ja adotadas.
- Mantenha SQL explicito com Dapper e queries organizadas por repository.
- Sempre aplicar filtro por organization_id em operacoes multi-tenant.
- Priorizar compatibilidade com o SYSTEM_DESIGN.md e COPILOT.md.
