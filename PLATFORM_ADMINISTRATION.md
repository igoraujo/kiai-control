# Administração da plataforma

O perfil `platform_owner` é o Administrador da Plataforma (Super Admin). Sua área é separada dos perfis `owner` e `admin` das academias. O acesso global acontece pelas rotas `/platform/*`, sem trocar o tenant da sessão ou conceder permissões operacionais de todas as academias.

## Painel

- `/platform/organizations`: busca, status e visões Em operação/Arquivadas/Todas; paginação; cadastro com primeiro owner e link direto para auditoria.
- `/platform/organizations/{id}`: dados, ativação/suspensão, contas e responsáveis; arquivamento/restauração; preservação legal; atalhos para auditoria e indicadores da academia.
- `/platform/organizations/{id}/audit`: histórico limitado à academia, incluindo arquivadas, com seleção fixa da organização.
- `/platform/audit`: consulta global por academia, autor, ação e período; dados pessoais ocultos na listagem; abertura direta dos detalhes com consulta registrada automaticamente.
- `/platform/operations`: saúde das dependências, requisições, taxas de erros, duração média/máxima, gráfico diário interativo, controle de contas agregado por academia, rotas e ocorrências.

Os dias escolhidos na tela são inclusivos e usam UTC. A API usa `from` inclusivo e `to` exclusivo, com intervalo máximo de 366 dias e até 200 itens por página. A interface usa 25 itens por página. O seletor de academia busca opções no servidor.

## Acesso e auditoria

- Todas as rotas da plataforma exigem `platform_owner` no servidor. Permissões locais e `X-Organization-Id` não dão acesso global.
- Novos responsáveis recebem owner ou admin. Alterações de acesso não concedem `platform_owner`; aluno/professor exigem os respectivos vínculos locais.
- A organização e a conta da plataforma são protegidas. O último owner ativo não pode ser bloqueado ou perder o papel de owner por este módulo. Um lock na organização serializa alterações concorrentes.
- Alterar status revoga refresh tokens e links de recuperação e incrementa a versão das sessões de todos os usuários da academia. A autenticação consulta o status da organização: suspensas/inativas não permitem login, refresh nem JWT anterior. Reativar permite novo login e não restaura sessões revogadas.
- Alterar e-mail/perfis/estado, redefinir senha e encerrar sessões invalida as sessões anteriores da conta. Senha temporária exige troca no próximo acesso.
- Cadastro da academia, provisionamento inicial do Super Admin e mutações da plataforma persistem a auditoria na mesma transação. Se a auditoria falhar, a alteração é revertida. Consultas globais também são auditadas antes de retornar dados.
- Motivo, valores anteriores/posteriores relevantes, autor e correlação são registrados; senhas e hashes não entram nos metadados.

A garantia transacional se aplica ao módulo da plataforma. Nas demais áreas, o middleware continua registrando a auditoria após a operação; sua evolução para transação/outbox permanece no plano.

## Arquivamento e restauração

Arquivar exige o nome exato da academia e uma justificativa. A operação marca `deleted_at`, define status inativo e revoga sessões/recuperações na mesma transação da auditoria. O tenant sai da lista de operação e continua consultável em Arquivadas e Todas. Cadastros, relações e histórico são preservados. Contas de uma academia arquivada não podem ser alteradas pelo módulo.

Ao retomar o contrato, abra **Academias → Arquivadas → Gerenciar → Restaurar e reativar**. Confirme o nome e o motivo. A operação remove a marca de arquivamento e torna a academia ativa na mesma transação da auditoria (`platform.organization.reactivated`), reutilizando o mesmo ID, contas, cadastros e histórico. Não cria outra academia. Os usuários liberados podem fazer novo login com suas credenciais existentes; contas bloqueadas continuam bloqueadas e sessões antigas permanecem inválidas.

A opção **Restaurar academia** mantém o cadastro inativo para revisão antes da ativação. Ambas exigem nome e motivo. Na API, `POST /organizations/{id}/restore` aceita `reactivate: true` para a retomada imediata; omitir essa opção mantém o comportamento de restaurar como inativa. A opção não é aceita na operação de arquivamento.

A organização interna da plataforma permanece protegida. Não há exclusão física em cascata de academias pela interface. Arquivamento é uma restrição operacional reversível, não uma resposta de eliminação de dados pessoais de um titular. Dados já eliminados por uma política de retenção ou atendimento ao titular não são recriados pela reativação.

## Privacidade da auditoria

O GET de auditoria retorna referência reduzida do autor, ação, organização e instante. Não retorna nome/e-mail do autor, IP, identificador completo da conta/alvo ou metadados. O filtro por autor continua no servidor, mas seu texto não é copiado para os registros de acesso.

Ao clicar em **Ver detalhes**, a janela carrega o evento automaticamente, sem solicitar justificativa nem outra confirmação. O POST `/audit-logs/{id}/details` recebe apenas `organizationId`, verifica o vínculo do evento com a academia e registra a consulta antes de entregar dados. Falha de auditoria impede a entrega. Os metadados são filtrados por lista de chaves permitidas, excluindo contatos, documentos, senhas, hashes, tokens e campos desconhecidos, inclusive aninhados. O acesso global segue exclusivo de `platform_owner`.

Referências reduzidas e remoção de identificadores não são apresentadas como garantia de anonimização irreversível. Dados pessoais ainda podem existir nos cadastros, backups e logs externos, sujeitos às políticas próprias do controlador.

## Métricas, readiness e retenção

A migration `016_platform_lifecycle_observability_privacy.sql` acrescenta `privacy_hold`, a marca de remoção de identificadores da auditoria e `platform_request_metrics`. Os scripts consolidados e o DBML representam o mesmo schema.

As métricas agrupam requisições por hora UTC, organização autenticada (ou alvo explícito de uma rota da plataforma), template de rota, método e status. Não coletam IP, usuário, nomes de alunos, dados de saúde, corpo, query string, token nem valores dos parâmetros de rota. Cabeçalhos de organização de usuários não autenticados são ignorados. A média é ponderada pela quantidade de requisições; o máximo é o maior observado, não um percentil. Períodos de indicadores têm limite de 90 dias; o controle por academia é paginado.

O buffer tem limite de cardinalidade e descarrega por transação em lotes. A telemetria é operacional e pode perder medições em um encerramento abrupto ou ao atingir o limite; não serve de registro financeiro ou cálculo de SLA. Falhas de persistência são enviadas ao logger e os agregados são recolocados no buffer limitado. Cada instância agrega seu tráfego; os upserts acumulam os resultados no banco compartilhado.

`/health/live` verifica o processo. `/health/ready` testa PostgreSQL e cache (Redis quando configurado, memória nos demais casos), sem expor detalhes de conexão. A interface consulta as mesmas dependências, com horário da aferição. Readiness responde 503 quando uma dependência necessária está indisponível; a interface autenticada também depende da disponibilidade do banco de autenticação. Monitoração externa continua necessária.

Configuração no ambiente da API:

| Variável | Padrão técnico |
| --- | --- |
| `PlatformPrivacy__MetricsRetentionDays` | 30 dias |
| `PlatformPrivacy__IncidentRetentionDays` | 90 dias |
| `PlatformPrivacy__AuditIdentityRetentionDays` | 180 dias |
| `PlatformPrivacy__FlushSeconds` | 15 segundos |

Os prazos aceitam 1 a 3650 dias; o flush, 1 a 300 segundos. São escolhas técnicas configuráveis, não prazos legais universais. O controlador deve definir e documentar finalidade, base legal e necessidade de conservação, incluindo obrigações específicas.

Um processamento periódico remove métricas/ocorrências vencidas e remove autor, alvo, IP, user agent e metadados dos eventos de auditoria vencidos, preservando o fato administrativo (academia, ação, contexto de entidade e instante). As alterações são auditadas por contagens e realizadas em transação, em lotes limitados. Nenhum cadastro ou registro financeiro é apagado por esse processamento.

A preservação legal de uma academia, com fundamento auditado, impede a limpeza automática dos seus registros. Deve ser revista periodicamente e encerrada quando cessar sua necessidade. Ao desativá-la, registros já vencidos ficam elegíveis no próximo processamento. Locks de organizações serializam a limpeza e as alterações de preservação. A rotina executa por hora, até dez lotes de 2000 itens por categoria; o logger avisa quando o limite é atingido para revisão de capacidade/backlog.

Os controles técnicos não substituem o atendimento dos direitos dos titulares nem validam, por si sós, a conformidade integral. A ANPD esclarece que há hipóteses legais de conservação e situações em que a eliminação deve ocorrer; portanto, arquivamento e eliminação precisam de tratamento distinto. Referência: [Perguntas Frequentes da ANPD](https://www.gov.br/anpd/pt-br/acesso-a-informacao/perguntas-frequentes), em especial dados pessoais e direitos dos titulares. Para controles de acesso e segurança: [guia da ANPD para agentes de pequeno porte](https://www.gov.br/anpd/pt-br/centrais-de-conteudo/materiais-educativos-e-publicacoes/anonimizado___guia_orientat-_seg_da_inf_p_atpp.pdf).

## Ocorrências

A migration `015_platform_administration.sql` adiciona `platform_incidents` e índices para auditoria global. O schema consolidado de produção e o DBML também incluem a tabela. Bancos existentes aplicam a migration; novos bancos podem usar os scripts consolidados.

São registradas respostas HTTP 5xx (`request_failure`), login com resposta 401 (`authentication_failure`) e falhas do middleware de auditoria (`audit_failure`). Cada ocorrência contém template da rota, método, status, academia/usuário autenticado quando disponíveis e correlação. Não armazena corpo, query string, cabeçalhos de autorização, senha, token ou mensagem bruta de exceção.

O histórico começa com a instalação do módulo. Se o PostgreSQL estiver indisponível, o registro nele é impossível e o erro vai para o logger do servidor. Não há reconstrução retroativa das métricas.

## Primeiro Super Admin

Configure as três variáveis juntas no ambiente da API:

```text
PlatformOwner__Email=<email pessoal do mantenedor>
PlatformOwner__FullName=<nome do mantenedor>
PlatformOwner__Password=<senha temporária que atenda à política>
```

O bootstrap usa a política compartilhada e o hasher da aplicação. Cria a organização interna e a conta global com troca obrigatória de senha. Nas inicializações seguintes, preserva a senha já definida. Um e-mail que pertence a outra conta não é promovido automaticamente; a inicialização falha para impedir apropriação acidental.

Depois de provisionar, remova as três variáveis de bootstrap. Guarde credenciais em um gerenciador de segredos, fora de arquivos versionados. Mantenha contas de academia separadas da conta de manutenção.

## API

| Método | Rota sob `/api/v1/platform` | Operação |
| --- | --- | --- |
| GET / POST | `/organizations` | Lista paginada / cadastro com primeiro owner |
| GET / PUT | `/organizations/{id}` | Consulta / alteração de dados |
| PUT | `/organizations/{id}/status` | Ativar, inativar ou suspender |
| DELETE | `/organizations/{id}` | Arquivar com confirmação e motivo |
| POST | `/organizations/{id}/restore` | Restaurar como inativa ou restaurar e reativar com `reactivate: true` |
| PUT | `/organizations/{id}/privacy-hold` | Preservação legal auditada |
| GET / POST | `/organizations/{id}/accounts` | Contas paginadas / novo responsável |
| PUT | `/organizations/{id}/accounts/{userId}` | E-mail, perfis e estado |
| PUT | `/organizations/{id}/accounts/{userId}/password` | Senha temporária |
| POST | `/organizations/{id}/accounts/{userId}/revoke-sessions` | Revogar sessões e recuperação |
| GET | `/audit-logs` | Auditoria global paginada |
| POST | `/audit-logs/{id}/details` | Consulta direta de detalhes com auditoria automática |
| GET | `/incidents` | Ocorrências paginadas |
| GET | `/overview` | Indicadores |
| GET | `/metrics` | Métricas agregadas, série diária e controle por academia |
| GET | `/health` | Saúde das dependências sem segredos |
| GET | `/retention-policy` | Prazos técnicos em vigor |

O GET de organizações retorna `{ items, total }`, alinhado com os demais catálogos paginados.
