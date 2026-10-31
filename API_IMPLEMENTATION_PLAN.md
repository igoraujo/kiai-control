# Kiai Control API - Pendencias e Plano de Implementacao

> **Documento historico, substituido em 2026-10-30.** Nao use o checklist abaixo como estado atual: varias pendencias listadas foram implementadas desde esta revisao e outras afirmacoes precisam de validacao. A fonte vigente para o projeto completo e [PROJECT_IMPLEMENTATION_PLAN.md](PROJECT_IMPLEMENTATION_PLAN.md). Preserve este arquivo apenas como registro do plano anterior.

**Atualizado:** 2026-10-04  
**Escopo:** API backend em `backend/kiai-control-api`  
**Referencia funcional:** `SYSTEM_DESIGN.md`, `COPILOT.md` e `CHECKPOINT.md`

Este documento e o checklist de execucao das pendencias da API. As caixas devem ser marcadas somente apos implementacao e validacao; itens de banco real nao contam como concluidos por build ou testes unitarios.

## Estado Atual

Ja existem endpoints para organizacoes, filiais, clientes, professores, modalidades, turmas, horarios, matriculas, professores por turma, aulas, chamada, financeiro e autenticacao. O seed inicial atribui `owner`, os JWTs carregam roles e os controllers aplicam autorizacao por papel. O fluxo financeiro inclui planos, assinaturas, faturas recorrentes e pagamentos.

Validacao local mais recente: `dotnet test KiaiControl.Api.slnx --no-restore`, executado em `backend/kiai-control-api`, com 31 testes passando. A migracao `database/kiai-control-database/migrations/001_initial_api_schema.sql` ainda nao foi executada em PostgreSQL e as queries Dapper nao possuem testes de integracao contra banco real.

## Ordem de Execucao

### Fase 0 - Banco Reproduzivel e Linha de Base

Prioridade: P0. Bloqueia a confianca nos repositorios SQL e nos fluxos financeiros.

- [ ] Disponibilizar PostgreSQL local de teste via Docker Compose ou Testcontainers.
- [ ] Aplicar `001_initial_api_schema.sql` em banco vazio; corrigir erros de DDL e divergencias com DBML.
- [ ] Aplicar o seed administrativo depois da migracao e validar login do usuario `owner`.
- [ ] Adicionar testes de integracao Dapper para CRUD, transacoes, constraints e isolamento entre duas organizacoes.
- [ ] Executar duas vezes a geracao de faturas e aulas no mesmo periodo e provar que a segunda chamada nao duplica registros.
- [ ] Adicionar uma rotina/registro de migracoes para impedir aplicacao fora de ordem e documentar como evoluir o schema.

**Aceite:** banco vazio pode ser provisionado do zero por comandos documentados; migracao e seed executam sem intervencao manual; testes de repositorio passam contra PostgreSQL real.

### Fase 1 - Autorizacao Administravel e Tenant

Prioridade: P0. A API ja restringe rotas por role, mas ainda nao ha API para administrar essas atribuicoes nem permissions granulares.

- [ ] Criar consultas e endpoints para consultar roles e permissions disponiveis.
- [ ] Criar endpoints administrativos para conceder/revogar role a uma pessoa dentro da organizacao autenticada.
- [ ] Impedir escalacao de privilegio: somente `owner` pode conceder `owner`; registrar ator, alvo e organizacao.
- [ ] Definir e persistir a relacao role-permission; aplicar policy/authorization handler nas operacoes, nao somente role na classe do controller.
- [ ] Escrever testes 401/403 para cada perfil e tentativas cross-tenant, inclusive manipulando `X-Organization-Id`.
- [ ] Definir o modelo de usuario multi-organizacao e fluxo de selecao de contexto. O modelo atual vincula o usuario a uma pessoa com uma unica organizacao.
- [ ] Adicionar rate limiting a cadastro publico, login, recuperacao de senha e refresh token.
- [ ] Remover o retorno de tokens de recuperacao em respostas de producao e integrar envio por canal transacional antes de habilitar reset publicamente.

**Aceite:** nenhum usuario comum consegue escolher organizacao, alterar roles/permissoes ou obter dados de outro tenant; todos os controllers sensiveis tem testes de autorizacao.

### Fase 2 - Auditoria de Acoes Criticas

Prioridade: P0 antes de ampliar operacoes financeiras ou de graduacao.

- [ ] Implementar gravacao append-only em `audit_logs` com ator, organizacao, acao, entidade, correlation id e metadata sem segredos/dados sensiveis.
- [ ] Auditar cadastro/alteracao de pessoa, grants de acesso, mudancas de fatura/pagamento, graduacao, cancelamento de aula e alteracoes de tenant.
- [ ] Criar endpoint paginado de consulta de auditoria, restrito a papeis autorizados e sempre filtrado por tenant.
- [ ] Adicionar testes para completude dos eventos, isolamento e imutabilidade logica.

**Aceite:** eventos criticos geram registro consistente; falha de auditoria nao deixa uma operacao financeira parcialmente aplicada; consulta nao cruza organizacoes.

### Fase 3 - Graduacao Marcial

Prioridade: P1. Depende de modalidades, clientes, professores e historico de presenca.

- [ ] CRUD de sistemas de graduacao por modalidade, sempre validando propriedade da modalidade pelo tenant.
- [ ] CRUD/reordenacao de niveis com criterios minimos e validacao de hierarquia unica.
- [ ] Registrar graduacao atual e eventos de promocao, regressao ou correcao com professor e justificativa.
- [ ] Consultar historico por cliente/modalidade.
- [ ] Calcular elegibilidade com tempo de treino, aulas/presencas, idade e avaliacao manual; deixar excecoes explicitas e auditadas.
- [ ] Expor relatorio de evolucao por periodo e modalidade.
- [ ] Testar regras de aniversario/idade, niveis fora de ordem, elegibilidade e isolamento entre tenants.

**Aceite:** promocao nao pode pular regra configurada sem permissao e justificativa; historico e preservado e elegibilidade e deterministica.

### Fase 4 - Evolucao Fitness

Prioridade: P1. Depende de clientes, modalidades e dados de presenca.

- [ ] CRUD paginado de metas por cliente, com modalidade opcional, tipo, alvo, unidade, inicio, prazo e status.
- [ ] Registrar progresso append-only; recalcular valor atual/status da meta em transacao.
- [ ] Validar tipo/unidade, alvo positivo, intervalo de datas e autorizacao de quem registra.
- [ ] Implementar resumo de progresso e categorias de evolucao com regras documentadas.
- [ ] Criar testes de progresso, conclusao, cancelamento, expiracao e acesso cross-tenant.

**Aceite:** registros historicos nao sao sobrescritos; percentuais/status derivam de dados consistentes e nao aceitam valores invalidos.

### Fase 5 - Gamificacao e Ranking

Prioridade: P1. Depende de chamada confirmada, privacidade do cliente e regras de pontos aprovadas.

- [ ] Definir por ADR a regra de pontos, streak, justificativa de ausencia, desempate, ciclo e recalculo.
- [ ] CRUD de ciclos de ranking e medals, com datas e escopo por organizacao/modalidade/turma.
- [ ] Calcular entradas a partir de presencas confirmadas e tornar recalculo idempotente.
- [ ] Registrar medalhas conquistadas sem duplicidade e respeitar `hide_from_ranking` nas respostas.
- [ ] Criar consultas de ranking atual/historico paginadas e tenant-scoped.
- [ ] Testar fechamento/arquivamento de ciclo, empate, privacidade e recalculo repetido.

**Aceite:** mesma fonte de presenca sempre produz o mesmo ranking; alunos ocultos nao aparecem em respostas publicas/autorizadas comuns.

### Fase 6 - Notificacoes

Prioridade: P2. Definir primeiro se o MVP entrega somente notificacao interna ou tambem push/e-mail.

- [ ] Criar notificacoes internas, listagem paginada, marcacao de leitura e filtros por status/canal.
- [ ] Impedir que uma pessoa consulte ou altere notificacoes de outra, salvo papel autorizado.
- [ ] Implementar processamento/retry para canais externos somente apos definir provedor, idempotency key e politica de falhas.
- [ ] Nao registrar tokens, mensagens privadas ou dados medicos em logs.
- [ ] Testar tenant, destinatario, transicoes de status e reprocessamento.

**Aceite:** notificacoes internas sao isoladas por destinatario/organizacao; envio externo e idempotente e observavel.

### Fase 7 - Relatorios e Dashboard

Prioridade: P2. Fazer apos contratos e regras dos modulos de negocio estarem estaveis.

- [ ] Definir contratos de KPIs e filtros comuns: periodo, modalidade, turma, cliente e professor.
- [ ] Implementar dashboard com clientes ativos/novos/cancelados, presenca e indicadores financeiros.
- [ ] Implementar relatorios de presenca, financeiro, desempenho e graduacao com SQL explicito e filtros por tenant.
- [ ] Validar datas, limites de intervalo, paginação e indices com dados representativos.
- [ ] Testar agregacoes com fixtures PostgreSQL e incluir cenarios de tenant cruzado.
- [ ] Adicionar teste de performance para consultas de maior custo; nao otimizar com cache antes de medir.

**Aceite:** valores batem com consultas de referencia; toda consulta filtra tenant e tem limites defensivos de periodo/paginacao.

### Fase 8 - Fechamento Operacional

Prioridade: P0 antes de producao. Executar em paralelo com as fases de dominio quando aplicavel.

- [ ] Revisar Problem Details e mapeamento uniforme de validacao, conflito, not-found e erros de banco.
- [ ] Revisar CORS, HTTPS, segredo JWT por ambiente, headers e logs sem dados sensiveis.
- [ ] Adicionar health check real para PostgreSQL e Redis, alem de verificacao de readiness.
- [ ] Configurar tracing/metricas/logs e correlation id nos fluxos novos.
- [ ] Atualizar OpenAPI, README da API, exemplos `.http` e documentacao de variaveis/seed.
- [ ] Fazer deploy de homologacao, executar smoke tests e ensaiar backup/restore antes da producao.

**Aceite:** checklist de seguranca aprovado; deploy e rollback documentados; health/readiness detectam indisponibilidade real das dependencias.

## Padrao de Entrega por Endpoint

- [ ] Confirmar regra de negocio e endpoint esperado no `SYSTEM_DESIGN.md`.
- [ ] Adicionar/ajustar entity e interface no Core, contratos Request/Response e validacao no UseCase.
- [ ] Escrever SQL explicito em `Repositories/Queries/<Context>` e filtrar sempre por `organization_id`.
- [ ] Registrar implementacao no DI e expor rota no controller do contexto existente.
- [ ] Testar regra de negocio e tenant isolation em unit tests; testar SQL/transacao/constraints em PostgreSQL real.
- [ ] Atualizar OpenAPI/README/checkpoint e executar `dotnet test KiaiControl.Api.slnx`.

## Definition of Done

- [ ] Contrato documentado e validado; erros retornam status HTTP coerentes.
- [ ] Autenticacao, papel/permissao e tenant sao verificados no servidor.
- [ ] Query Dapper usa parametros e tem filtro tenant explicito.
- [ ] Escritas financeiras/auditoria usam transacao quando varias tabelas participam.
- [ ] Unit tests cobrem regra feliz, validacao, not-found/conflito e isolamento.
- [ ] Integration tests PostgreSQL cobrem schema, query, constraint e concorrencia relevante.
- [ ] API, checkpoint e README atualizados; sem novos warnings/errors relevantes.

## Riscos e Decisoes Bloqueantes

- O desenho nao especifica formula exata de pontos/streak, desempate, categoria fitness ou excecoes de graduacao; definir regras antes de implementar os calculos.
- O suporte a multiplas organizacoes por conta requer rever o modelo atual `persons`/`user_accounts`, claims e selecao de contexto; nao resolver apenas aceitando qualquer header.
- A migracao atual e baseline sem historico de versoes executado. Antes de adotar em ambiente com dados, definir estrategia de rollout e migracoes incrementais.
- O cadastro publico de organizacao precisa de rate limiting e protecao contra abuso antes de ficar acessivel em producao.