# Integração mobile — contratos e decisões

Análise e implementação em 2026-11-16. Base: `/api/v1`. Consulte o [guia do app](frontend/kiai-control-mobile/README.md) para executar e configurar os SDKs nativos.

## Estado encontrado e preservação

O app Flutter tinha cliente HTTP, armazenamento seguro, navegação exclusiva aluno/professor, agenda, check-in, chamada e metas fitness. A API é um monólito modular .NET 10 com Contracts/Core/UseCases/Repositories/Services, Dapper/PostgreSQL, JWT com rotação de refresh e RBAC por organização. O banco já armazenava pessoas, alunos/professores, modalidades/horários, presenças pendentes/validadas, graduação, metas/registros, ciclos de ranking, medalhas e notificações internas.

Foram preservados os fluxos e modelos úteis. A substituição das duas shells e do seletor de papel local remove uma restrição incompatível com múltiplos perfis e perfis administrativos. A sessão agora é restaurada consultando a API; nenhum papel persistido determina autoridade. Uma shell comum usa capacidades derivadas no servidor. O cliente HTTP foi ampliado para corrigir rotação concorrente, respostas inválidas, expiração global e confirmação de mutações.

## Mapa das funcionalidades

| Recurso | Contrato existente utilizado | Complemento implementado |
| --- | --- | --- |
| Sessão | `POST /auth/login`, `/refresh`, `/logout`, `/change-password`; `GET /auth/me` | Contexto mobile com papéis, permissões e capacidades. Senha temporária exige troca e novo login. |
| Perfil | Pessoas/alunos/professores administrativos; `GET /mobile/profile` e `/mobile/teacher/profile` | Leitura/edição pessoal e foto sem aceitar ID arbitrário. |
| Modalidades/aulas | `GET /mobile/modalities`, `/mobile/lessons`; `/mobile/teacher/lessons` | Semana padrão no fuso da academia e `canCheckIn` calculado pela API. |
| Chamada | `/mobile/teacher/lessons/{id}/eligible-clients`, `/attendances` GET/PUT | Preservada, com verificação da resposta no app. Professor avulso também acessa aula sem horário recorrente. |
| Check-in | `POST /attendance/check-in`, `DELETE /attendance/check-in/{lessonId}` | Sempre pendente, inclusive diante da configuração legada sem confirmação. |
| Validação | `GET /attendance/pending`, `PATCH /attendance/{id}/validate` | Tela mobile; informações da aula na fila; verificações de escopo e impedimento de autoaprovação. |
| Evolução/conquistas | `GET /portal/journey`, `/mobile/fitness`, `/portal/goals/{id}/progress` | Totais/meses/graduação/sequência em `/mobile/progress`, sem contar pendências. |
| Ranking | Ciclos administrativos, entradas e medalhas já existentes | Ranking pessoal anual/total, calculado diretamente das presenças confirmadas, sem depender do recálculo de um ciclo. |
| Dashboard | `GET /reports/dashboard?from=YYYY-MM-DD&to=YYYY-MM-DD` | Uso autorizado em mobile; alunos usam progresso e professores usam suas aulas. |
| Notificações | `GET /notifications`, `PATCH /notifications/{id}/read`, `POST /notifications` | Detalhe pessoal, registro de dispositivos, consentimento e entrega FCM persistida. |

## Novos endpoints

Todos exigem JWT válido e organização/pessoa das claims. Os endpoints pessoais não aceitam `organizationId`, `personId` ou `clientId` para mudar o alvo. As mutações bem-sucedidas retornam 204, salvo check-in/validação que mantêm seus contratos existentes.

### `GET /mobile/context`

```json
{
  "organizationName": "Nome cadastrado da academia",
  "timezone": "America/Sao_Paulo",
  "roles": ["client"],
  "permissions": ["attendance.check-in", "notifications.read"],
  "capabilities": ["student.lessons", "student.progress", "notifications.read"],
  "mustChangePassword": false
}
```

Capacidades possíveis nesta versão: `student.lessons`, `student.progress`, `student.graduation`, `teacher.lessons`, `attendance.validate`, `reports.dashboard`, `notifications.read`. Vínculos ativos e regras dos controllers são verificados no servidor, além das permissões. Novos papéis não bloqueiam o login; recebem os recursos autorizados pelo backend. O app sempre oferece perfil/logout. Esta leitura é permitida durante a exigência de troca de senha, mas os demais dados continuam bloqueados pelo middleware.

### `GET /mobile/account`, `PUT /mobile/account`

Leitura: `fullName`, `preferredName`, `email` de contato, `phone`, `hideFromRanking`, `pushEnabled`, `photoBase64` e `photoContentType` opcionais. Atualização: os seis primeiros campos, sem campos de conta/papel ou de login. Nomes/e-mail/telefone são normalizados e validados pelo UseCase; conflito de e-mail retorna 400 com mensagem legível. Não há atualização de permissões pelo app.

### `PUT /mobile/account/photo`, `DELETE /mobile/account/photo`

PUT recebe `{ "base64": "conteúdo-base64", "contentType": "image/jpeg" }`. Tipos aceitos: JPEG/PNG, até 524288 bytes. API limita corpo e tamanho e verifica assinatura binária. Foto armazenada em `bytea`; não é publicada em URL aberta. GET account devolve a foto pessoal autenticada. DELETE remove os dois campos.

### `GET /mobile/progress`

Exige papel `client` e vínculo ativo resolvido no servidor. Retorna `year`, `totalClasses`, `yearClasses`, `currentStreak`, `totalPosition`, `yearPosition`, `months: [{month, attendanceCount}]`, `graduations: [{modalityName, beltName, degreeNumber, nextLevelName, lessonsRemaining, requiresManualEvaluation}]`.

- **Presença contabilizada:** aula `completed`, presença `present`/`late`, `validated_at` e validador diferente do próprio aluno. Registros legados sem comprovação de validação ou autoaprovados não entram nos indicadores. O histórico mantém os estados originais.
- **Ano:** ano corrente no fuso IANA configurado pela academia. Instantes próximos da virada do ano são convertidos antes da contagem.
- **Meses:** meses com registros nos últimos 12 meses; a interface mostra vazio quando não existem, sem inventar registros.
- **Sequência:** sessões concluídas das modalidades atualmente vinculadas, desde a entrada do aluno, em ordem de início e ID; presença pendente, não validada ou falta interrompe a sequência. A plataforma usa aulas abertas por modalidade e não possui obrigação de comparecimento por horário individual. Por isso a sequência segue essa definição explícita, também apresentada na tela.
- **Graduação:** somente modalidades `martial_art`. Próximo nível é o próximo na hierarquia do sistema atual; aulas restantes usam o critério `min_lessons` e presenças após a data de concessão do nível, conforme o critério de calendário já usado na API de graduação. Aulas suficientes não concedem promoção automática: dias, percentual, idade, avaliação e autorização continuam sob responsabilidade da API/academia.
- Posição nula quando o aluno está oculto ou não participa do conjunto público de alunos ativos.

### `GET /mobile/rankings?yearOnly=true&page=1&pageSize=25`

Exige aluno ativo. Retorna lista de `{clientName, position, attendanceCount, isMe}`. `yearOnly=false` consulta o total. Lista limitada à mesma academia, alunos ativos e pessoas não excluídas/não ocultas. Não expõe IDs, contatos ou dados de outras pessoas. Empates usam `rank()` sobre o número de aulas; a ordem da lista é estável. PageSize até 200; page/size inválidos retornam 400. O app oferece paginação de 25.

### `PUT /mobile/devices`, `DELETE /mobile/devices/{installationId}`

PUT: `{installationId, token, platform}`, com `platform` em `android|ios`, instalação aleatória por aparelho e alvo de envio Firebase no campo `token`. Com o SDK atual, esse alvo é o FID recebido por `onRegistered`, usado em `Message.Fid` no Admin SDK. Atualizar uma instalação transfere o vínculo para a sessão autenticada atual; entregas antigas não atravessam pessoa/organização. DELETE restringe a instalação à pessoa/academia autenticadas. O dispositivo armazena também a versão de sessão atual da conta; bloqueio e revogação/troca de senha impedem novos envios até novo registro autenticado. Tokens/alvos não são colocados em logs, auditoria de corpo ou código fonte.

### `GET /notifications/{id}`

Mesma permissão `notifications.read` da lista. Retorna `NotificationResponse` apenas do destinatário autenticado, após `scheduledAt`; outro destinatário/academia retorna 404. O app abre o conteúdo novamente na API ao tocar no push; não confia em título/rota arbitrária do payload. Leitura via PATCH tornou-se idempotente.

## Ajustes em contratos existentes e autorização

`GET /mobile/lessons` e alias `/portal/lessons` preservam parâmetros `from`/`to` com offsets e limite de 31 dias. Sem intervalo, retornam até a próxima segunda-feira à meia-noite no fuso da academia. Não retornam aulas já iniciadas. `canCheckIn` incorpora habilitação da academia, permissão efetiva, horário e ausência de registro. `canCancelCheckIn` é exclusivo de pendências antes do início. O app usa essas flags; o relógio do aparelho não decide validade.

O INSERT de check-in é atômico, restringe matrícula ativa, modalidade/academia, início da aula e unicidade. O instante de check-in é gerado pela API, não recebido do aluno. Offsets são comparados como instantes PostgreSQL `timestamptz`. Duplicação/prazo expirado retorna 409; a interface não informa sucesso para resposta vazia, inválida ou autoaprovada.

A fila de pendências inclui `modalityName` e `lessonStartsAt`. Validação aceita `present`, `late` ou `absent`; registra validador/data e exige estado ainda pendente, impedindo dupla validação. Autoaprovação não altera o registro. As policies existentes continuam necessárias. O middleware de escopo do professor foi mantido; o controller também verifica as aulas original/destino nas alterações. A consulta de detalhes agora usa a mesma regra da listagem para professores avulsos em `lesson_instructors`, mesmo sem `modality_schedule_id`.

`organization_settings.require_teacher_confirmation` permanece no contrato por compatibilidade, mas a leitura da API informa `true` e a gravação rejeita `false` com mensagem explícita; a migration atualiza configurações antigas. A flag `allow_client_checkin` continua respeitada. Queries de jornada, ranking, relatórios e critérios de graduação foram alinhadas para excluir pendências e validações não comprovadas dos indicadores.

## Persistência e entrega push

Migration `017_mobile_accounts_and_push.sql`: dois campos e constraints de foto em `persons`; tabelas `mobile_preferences`, `mobile_devices` e `push_deliveries`; índices e normalização da confirmação. Snapshot idempotente de produção e DBML atualizados. A migration deve ser aplicada pelo processo normal do projeto; não foi aplicada ao banco de trabalho do usuário.

`POST /notifications` mantém `channel=push`; agora o registro permanece `pending` até aceitação real pelo provedor. Mensagem push limita título/texto a 3000 bytes UTF-8 para reservar espaço ao envelope FCM. O worker opcional consulta a cada 15 segundos, respeita destinatário/tenant ativo, consentimento, agendamento e leitura, enfileira por dispositivo e adquire lease com `FOR UPDATE SKIP LOCKED`. Até cinco tentativas com lease de dois minutos; falha esgotada fica `failed`. Dispositivo inválido é removido. `sent_at` registra aceitação, não entrega comprovada. Como o provedor não participa da transação PostgreSQL, existe possibilidade de reenvio se o processo cair após aceitação e antes de gravar o resultado. O app sempre recarrega o conteúdo pelo ID.

Apenas notificações push com data de envio prevista nos últimos 30 dias entram na descoberta da fila; não há replay ilimitado de comunicados antigos. Não foram adicionados serviços automáticos para inventar notificações de promoção/aula. Canais de e-mail de comunicados mantêm seu contrato; o serviço SMTP existente de recuperação de senha não é usado como entrega fictícia de push.

## Limitações verificadas e configurações externas

- O banco possui metas e registros fitness, mas não um modelo de ficha de musculação/exercícios/séries/cargas prescritas. O app mostra as metas existentes; uma ficha detalhada exige novo contexto de treino, contratos e prescrição autorizada. Não há mocks de exercícios.
- A graduação só fornece previsão quando há sistema, nível atual e próximo nível cadastrados; a interface trata ausência e último nível.
- Não há recurso de destino associado em notificações existentes além do próprio comunicado. O toque abre o comunicado autenticado. Navegar diretamente a uma aula/meta exige adicionar tipo/ID de recurso ao contrato e validar vínculo no servidor.
- Não há credenciais Firebase/APNs, conta de serviço ou assinatura de loja no código. Envio externo e comportamento em aparelho real dependem da configuração do proprietário descrita no README.
- iOS exige Xcode completo e provisionamento; ferramentas de linha de comando isoladas não produzem build/teste de iOS. A assinatura Android de produção precisa substituir a configuração de debug pré-existente.
- Não há publicação em loja ou deploy de API/banco nesta entrega.

## Verificações

O projeto contém testes de transporte/sessão e widgets Flutter, além de testes PostgreSQL de perfil/foto, permissões/capacidades, virada de ano no fuso da academia, ranking, isolamento, confirmação, professor avulso, consentimento/fila push e contratos HTTP com JWT real. O teste HTTP também verifica senha temporária, revogação após troca, 403 para validação por aluno e resistência a IDs de outra pessoa no corpo do perfil.

Os resultados executados estão registrados em `PROJECT_IMPLEMENTATION_PLAN.md`; ter código compilável não comprova entrega externa FCM/APNs nem assinatura/distribuição nativa.
