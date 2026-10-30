# Kiai Control - Plano Geral de Implementacao

**Estado verificado em:** 2026-10-30  
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

O backend ja tem uma base ampla de API e modulos de negocio. O Admin cobre uma parte dos fluxos operacionais. O banco tem schema inicial, duas migrations incrementais e script de bootstrap. A principal lacuna nao e a ausencia de todos os modulos: e a falta de verificacao ponta a ponta contra PostgreSQL, de cobertura automatizada real em alguns projetos e de paridade do painel com as capacidades existentes da API.

O workspace atual nao contem projetos mobile, infraestrutura/deploy ou workflows CI. Nao considere o produto pronto para producao com base apenas em build ou em controllers existentes.

### Atualizacao de Implementacao (2026-10-30)

Esta atualizacao prevalece sobre itens de status conflitantes nas tabelas abaixo:

- JWT HS256 valida issuer, audience, assinatura e expiracao; a chave de ao menos 32 bytes e obrigatoria fora de Development. Fallback de desenvolvimento foi removido do `appsettings.json`.
- Claims de permissions sao carregadas no login/refresh e exigidas por policies nos controllers, em conjunto com as roles existentes.
- Migrations `004` e `005` definem grants globais por role e overrides por organizacao; os endpoints de alteracao usam tenant/ator do token, e a role `owner` nao pode perder suas permissions pela API.
- Login, refresh, logout e operacoes de senha usam limite de 10 requisicoes por IP/minuto. Recuperacao nao expoe token e retorna mensagem generica; envio por e-mail continua pendente.
- Admin tem paginas `/access` e `/billing`, navega por permissions e oferece vinculo de modalidades no formulario do aluno; o projeto passou build/test localmente.
- Em 2026-10-30 passaram 49 testes unitarios da API e 6 testes do Admin; tambem passaram testes de policy/bootstrap sem banco. Nao foram executadas conexoes PostgreSQL por ausencia de Docker e database local.

## Inventario Verificado

| Area | Estado atual | Evidencia e limite |
|---|---|---|
| API e arquitetura | Presente, validacao operacional incompleta | Projetos Api, Core, Contracts, UseCases, Repositories e Services; controllers para autenticacao, organizacoes, unidades, clientes, professores, modalidades, turmas, aulas, presenca, financeiro, fitness, graduacao, ranking, notificacoes, relatorios, auditoria e controle de acesso. A existencia das camadas/rotas nao comprova queries e fluxos em PostgreSQL real. |
| Autenticacao | Melhorada; entrega de reset pendente | Access JWT valida issuer/audience, assinatura obrigatoria HS256, expiracao e role claim explicita; chave de 32 bytes e fallback somente em Development. Roles/permissoes efetivas entram no JWT no login/refresh. Login, refresh, logout e fluxos de senha tem rate limit de 10/IP/minuto. Recuperacao responde com mensagem generica sem token, mas nao entrega link por e-mail. |
| Tenant e autorizacao | Policies e overrides por tenant implementados; schema pendente | Middleware prioriza a organizacao do token sobre `X-Organization-Id`. Controllers combinam roles e permission claims; `role_permissions` define defaults globais e `organization_role_permissions` overrides locais. Owner nao pode ter suas permissions alteradas pela API. `persons` ainda representa uma organizacao por conta. Migrations `004` e `005` ainda nao foram executadas em PostgreSQL. |
| Auditoria | Parcial | Middleware e endpoint de consulta existem. A gravacao ocorre depois da resposta da mutacao e falhas sao capturadas; assim, uma operacao pode concluir sem auditoria persistida. Integridade/atomicidade nao foi comprovada. |
| Banco de dados | Base presente, execucao real nao comprovada | `001_initial_api_schema.sql`, migrations `002`-`005`, seed administrativo e `create_database.sh`. Nao foi encontrado ledger/registro de migrations nem teste de repositorio executado contra PostgreSQL. |
| Testes do backend | Unitarios validados; integracao PostgreSQL pendente | Em 2026-10-30, 49 testes unitarios passaram; middleware e bootstrap/policy foram validados sem banco. O projeto IntegrationTests inclui round-trip DateOnly e isolamento de overrides por tenant em Testcontainers; nenhum deles foi executado porque esta maquina nao tem Docker/PostgreSQL. A fixture aplica migrations `001`-`005`. `PerformanceTests` segue fora da solution ate existir benchmark real. |
| Admin | Parcial | Paginas existentes: dashboard, clientes, professores, unidades, modalidades, turmas, aulas, check-ins pendentes, acesso/permissoes e financeiro (planos, assinaturas, faturas e pagamentos). O formulario de aluno sincroniza modalidades e a tela de turma matricula alunos; a jornada continua distribuida entre essas telas. Ainda faltam fitness, graduacao, ranking/medalhas, notificacoes, auditoria e relatorios operacionais. |
| Testes do Admin | Cobertura inicial validada | `KiaiControl.Admin.UnitTests` possui 6 testes aprovados: refresh/retry com tenant, erro HTTP, nome obrigatorio, navegacao por permissions e bloqueio de `/access` sem permission. Faltam testes autorizados da tela de acesso, financeiro, vinculo de modalidades e fluxos E2E. |
| Mobile | Fora do workspace | Nao ha pasta/projeto Flutter presente, embora esteja no escopo de `SYSTEM_DESIGN.md`. |
| Infraestrutura e CI/CD | CI parcial presente | Workflow GitHub Actions compila API/Admin, executa testes e publica TRX; integration tests usam Docker/Testcontainers. Nao foram encontrados Dockerfiles, Compose, deploy manifests ou configuracao de observabilidade. A primeira execucao remota ainda nao foi confirmada. |
| Health e observabilidade | Parcial | Correlation ID e logging de falha de auditoria existem. Ha `/health/live` e `/health/ready`, mas o registro usa `AddHealthChecks()` sem checks de PostgreSQL/Redis. Nao foi encontrada integracao OpenTelemetry/Prometheus/Grafana. |

## Alinhamento dos Documentos

- `SYSTEM_DESIGN.md` permanece como referencia de arquitetura e escopo desejado; seus requisitos de mobile, observabilidade e deploy ainda nao estao presentes no workspace.
- `CHECKPOINT.md` e um snapshot de 2026-10-08. A afirmacao de 31 testes aprovados nao substitui a execucao atual; os projetos de teste contem placeholders.
- `API_IMPLEMENTATION_PLAN.md` e de 2026-10-04 e esta desatualizado: controle de acesso, auditoria, fitness, graduacao, ranking, notificacoes e relatorios ja possuem controllers/camadas, e existem migrations posteriores a `001`.
- O README da API dizia que os endpoints eram apenas smoke endpoints; isso nao representa mais a estrutura atual. O README do Admin sugeria `dotnet test` apesar de nao existir projeto de teste.
- `COPILOT.md` e `.agent.md` devem direcionar os agentes para este plano antes de evoluir o produto.

## Plano de Evolucao

### Fase 0 - Linha de Base Confiavel (P0)

- [ ] Revalidar a solution completa da API em runner com Docker depois da migration `005`; localmente passaram 46 unit tests e o bootstrap/policy isolado.
- [x] Gerar e validar localmente um TRX independente por projeto ativo: API em `backend/kiai-control-api/TestResults/{unit,integration,architecture}` e Admin em `frontend/kiai-control-admin/TestResults/admin`.
- [ ] Confirmar que a pipeline publica os TRX como artifact apos sua primeira execucao no GitHub Actions.
- [x] Substituir os smoke tests `Assert.True(true)` por verificacoes reais ou removê-los ate existir comportamento testavel. O teste de arquitetura compara `ProjectReference` com o grafo permitido; o teste de bootstrap resolve repositorios registrados no DI; placeholders sem comportamento foram removidos.
- [x] Criar workflow de CI para restore/build da API e Admin, testes por projeto e falha quando TRX esperado estiver ausente, vazio ou com falhas. Os comandos de teste e os caminhos TRX foram executados localmente; publicacao do artifact aguarda a primeira execucao no runner GitHub.
- [ ] Confirmar a primeira execucao do workflow em runner limpo e a publicacao dos tres artifacts TRX.
- [x] Criar projeto de testes do Admin e cobrir refresh, tenant, nome obrigatorio, erro HTTP, navegacao por permissions e bloqueio de `/access` sem permission; 6 testes passaram localmente e nao ha pacote vulneravel no projeto.
- [ ] Cobrir a tela de gestao de permissions, estados de erro/carregamento e fluxos E2E criticos.
- [ ] Atualizar a matriz de estado deste documento quando uma validacao mudar o status de uma area.

**Aceite:** pipeline reproduzivel em ambiente limpo, relatorios TRX preservados por projeto, contagem de testes visivel, nenhum placeholder contado como cobertura e documentos sem afirmacoes contraditas pelo codigo.

### Fase 1 - PostgreSQL, Migrations e Integracao (P0)

- [x] Implementar fixture Testcontainers com PostgreSQL 17 que aplica migrations `001`-`005` em ordem e descarte a instancia ao final; harness compilado localmente.
- [ ] Executar a fixture em runner com Docker e validar a conexao real, migrations e round-trip de `DateOnly`. Esta maquina nao tem Docker/PostgreSQL; build nao satisfaz este aceite.
- [ ] Documentar o comando de execucao em ambiente com Docker e configurar o runner de CI para executar o teste de integracao.
- [ ] Aplicar as cinco migrations e o seed em banco vazio; validar schema, bootstrap e login administrativo.
- [ ] Definir controle de versao/aplicacao de migrations (ledger ou ferramenta escolhida), ordem, rollback e comportamento em banco existente.
- [ ] Adicionar testes de integracao para os repositorios Dapper: CRUD, constraints, datas `DateOnly`, transacoes e isolamento entre organizacoes.
- [ ] Validar os fluxos completos de criacao/edicao de cliente e professor, chamadas, matriculas, pagamentos e consultas diretamente contra PostgreSQL.
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
- [ ] Entregar link/token de reset por canal seguro (provedor de e-mail ainda nao configurado) e limitar tentativas por IP/conta.
- [x] Limitar login, refresh, logout, change-password e reset a 10 requisicoes por IP/minuto.
- [ ] Configurar limites por ambiente e aplicar rate limiting aos demais endpoints publicos.
- [x] Remover a chave JWT fallback do appsettings geral; validar issuer/audience, lifetimes e chave HS256 >= 32 bytes; usar fallback apenas em Development e falhar startup em outros ambientes quando a chave estiver ausente.
- [ ] Auditar mutacoes criticas com garantia de persistencia coerente com a operacao (transacao ou outbox); cobrir falha de auditoria e isolamento por tenant.
- [ ] Revisar logs e artefatos de excecao para remover `Authorization`, JWTs, refresh/reset tokens e dados pessoais. Um artefato de excecao compartilhado durante a analise continha header `Authorization`; remover/guardar o artefato e revogar o token se ainda puder ser valido.
- [ ] Completar testes 401/403, tenant cruzado, enumeracao de contas, rotacao/reuso de refresh e reset de senha.

**Aceite:** autorizacao efetiva e testada no servidor; segredo ausente impede configuracao insegura; recuperacao nao vaza token; logs nao armazenam credenciais; auditoria critica e duravel.

### Fase 3 - Operacao e Prontidao para Producao (P0 antes do deploy)

- [ ] Registrar health checks reais de PostgreSQL e Redis; `/health/ready` deve falhar quando dependencia necessaria estiver indisponivel.
- [ ] Padronizar tratamento de excecoes/Problem Details em todos os ambientes e revisar mensagens que possam revelar detalhes internos.
- [ ] Implementar OpenTelemetry para traces, metricas e logs; definir correlation/trace IDs e dashboards/alertas operacionais.
- [ ] Criar Dockerfiles/Compose para desenvolvimento e pipeline de build/publicacao/deploy; documentar variaveis sem valores secretos.
- [ ] Automatizar backup e testar restauracao do PostgreSQL; documentar rollback de deploy e migrations.
- [ ] Fazer deploy de staging e smoke tests autenticados da API e do Admin antes de declarar release.

**Aceite:** readiness representa dependencias reais; backup foi restaurado em teste; deploy/rollback e observabilidade sao demonstrados em staging.

### Fase 4 - Fechar os Fluxos do Painel Admin (P1)

- [ ] Unificar a jornada de aluno entre cadastro/modalidades, assinatura financeira e matricula em turma; hoje cada etapa pode ser concluida em sua tela existente, mas sem fluxo guiado unico nem historico/detalhe consolidado.
- [ ] Completar a jornada de professor: associacao a modalidades/turmas e consulta de agenda.
- [x] Implementar telas para planos, assinaturas, faturas e pagamentos, com confirmacao de cancelamento/desativacao e validacao de saldo/pagamento; build Admin aprovado, aguardando validacao com API/PostgreSQL real.
- [ ] Implementar interfaces para graduacao, metas/progresso fitness, rankings/medalhas, notificacoes, relatorios e auditoria conforme papeis autorizados.
- [ ] Integrar os relatorios existentes ao dashboard e permitir filtros previstos pelo contrato.
- [ ] Revisar navegacao por role/permission, estados vazios, carregamento, erros, paginacao e responsividade em todos os fluxos.
- [ ] Cobrir fluxos criticos com testes de componente e E2E: login, criar/editar aluno, matricular, registrar presenca, validar check-in e registrar pagamento.

**Aceite:** cada fluxo operacional do MVP pode ser concluido pelo Admin sem chamadas manuais/API externa; autorizacao visual acompanha a autorizacao do backend; E2E dos fluxos criticos passa.

### Fase 5 - Regras de Dominio e API (P1)

- [ ] Comparar cada modulo ja existente com os requisitos RF/UC de `SYSTEM_DESIGN.md`; registrar lacunas sem recriar controllers/CRUDs existentes.
- [ ] Formalizar por decisao tecnica as regras ainda ambiguas de graduacao, elegibilidade, pontos, streak, desempate, ciclos de ranking, categorias fitness e excecoes administrativas.
- [ ] Cobrir regras de dominio com unit tests e queries/constraints com integracao PostgreSQL, incluindo concorrencia e repeticao de comandos.
- [ ] Conferir contrato/OpenAPI e erros HTTP para cada fluxo; garantir paginacao e limites defensivos nas listagens/relatorios.
- [ ] Validar privacidade `HideFromRanking`, estados de aluno/professor e restricoes por papel em todas as respostas/relatorios.

**Aceite:** requisitos aplicaveis tem regra explicita, contrato, validacao e testes; nao ha comportamento de negocio inferido somente da interface ou de documentacao antiga.

### Fase 6 - Aplicativo Mobile (P2, escopo de produto)

- [ ] Criar o projeto Flutter em `mobile/kiai-control-app` ou documentar formalmente a decisao de retirar mobile do MVP.
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

Analise baseada no codigo e arquivos presentes no workspace em 2026-10-30, incluindo controllers, camadas de aplicacao, paginas do Admin, migrations e projetos de teste. Nao foi possivel comprovar execucao real contra PostgreSQL, deploy, restauracao de backup ou funcionamento de servicos externos; esses pontos permanecem abertos ate evidencia automatizada ou operacional.