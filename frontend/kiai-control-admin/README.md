# Kiai Control Admin

O painel do Administrador da Plataforma inclui academias, responsáveis, auditoria global e ocorrências. Consulte [PLATFORM_ADMINISTRATION.md](../../PLATFORM_ADMINISTRATION.md) para o acesso inicial e as regras de operação.

Painel web em .NET 10, Blazor Server e MudBlazor. Consome a API versionada e compartilha somente `KiaiControl.Contracts` com o backend.

## Executar

```bash
dotnet run --project backend/kiai-control-api/src/KiaiControl.Api/KiaiControl.Api.csproj
dotnet run --project frontend/kiai-control-admin/src/KiaiControl.Admin/KiaiControl.Admin.csproj
```

Configure `Api__BaseUrl` no Admin. O padrão é `http://localhost:5210`. Aplique as migrations `001`–`014` antes de iniciar a API atualizada. O banco local `dev` recebeu `011`–`014` nesta entrega, preservando os registros. A `014` cria as configurações ausentes de academias antigas para evitar falha no carregamento da agenda do aluno.

## Áreas e navegação

| Rota | Experiência |
|---|---|
| `/` | Dashboard e relatórios de gestão |
| `/fight` | Entrada Fight: artes marciais, graduação, frequência e conquistas |
| `/gym` | Entrada Gym: metas, progresso, frequência e treinamento |
| `/student` | Área do aluno: agenda/check-in, presença, graduação, metas, conquistas e financeiro próprio |
| `/teacher` | Agenda pessoal e acesso às chamadas do professor |
| `/people` | Diretório de pessoas/equipe, vínculos de aluno/professor e gestão de contas existentes |
| `/access` | Criação de acessos e permissões por perfil |
| `/organization` | Dados da academia, fuso e política de check-in |
| `/account` | Perfil pessoal e alteração voluntária de senha |
| `/forgot-password`, `/reset-password` | Solicitação e conclusão da recuperação de acesso |

Gym e Fight usam identidade visual, ícones e entradas distintas. As visões **Geral**, **Fight** e **Gym** estão disponíveis para owner, admin e recepção conforme suas permissões. Alunos, professores, modalidades, aulas e metas recebem o filtro no servidor; busca, paginação e totais respeitam a visão. O seletor e os atalhos mantêm a área na URL, inclusive após atualizar a página.

Um aluno pertence às áreas das modalidades vinculadas: `martial_art` → Fight, `fitness` → Gym e `hybrid` → ambas. Quem pratica nas duas áreas aparece em ambas e uma única vez na visão Geral. Alunos sem modalidade aparecem somente na Geral; os vínculos com modalidades inativas continuam permitindo consultar o histórico por área. A tabela identifica as áreas de cada aluno. O cadastro e a jornada da pessoa permanecem únicos. O financeiro e o dashboard são gerais.

As metas com modalidade explícita seguem a área dessa modalidade; metas sem modalidade seguem as áreas do aluno. O seletor de alunos da graduação considera Fight. A recepção continua sem acesso a metas, graduação e financeiro. As capacidades das modalidades ativas atualizam os atalhos do menu após criar/alterar/desativar uma modalidade.

`AdminNavigation` concentra a matriz de papéis/permissões usada no menu e na proteção das rotas. O destino do login depende do perfil; alunos e professores não são direcionados ao dashboard administrativo. Pessoas com ambos os vínculos podem alternar suas áreas pelo menu. O modelo atual mantém uma organização por conta.

## Cadastros e contas

O diretório cadastra funcionários sem exigir vínculo de aluno/professor. A ação **Gerenciar** e a seleção **Pessoa já cadastrada** nos formulários de aluno/professor adicionam esses vínculos à mesma pessoa, preservando sua identidade e histórico.

É possível criar conta com senha temporária, alterar o e-mail de login, conceder/remover papéis, bloquear/reativar acesso, encerrar sessões e redefinir a senha temporária. A própria conta e contas superiores são protegidas nas ações administrativas. Alterações sensíveis invalidam sessões; o token é verificado contra o estado atual da conta e as permissões do banco.

Senhas novas e temporárias exigem no mínimo 6 caracteres, uma letra maiúscula, uma minúscula e um caractere especial. Cadastro, primeiro acesso, Minha conta e recuperação exibem a mesma orientação e usam `PasswordPolicy`, compartilhada com a API.

As permissões `clients.read`, `teachers.read`, `branches.read` e `modalities.read` permitem consulta sem conceder gravação. As respectivas permissões `.manage` autorizam as operações de gestão. As opções configuráveis respeitam os papéis suportados pela API; a permissão do owner é fixa.

As principais tabelas de cadastro e aulas usam paginação no servidor; a busca consulta o conjunto completo. Financeiro, notificações, rankings, metas e históricos possuem controles de paginação. As listas auxiliares usadas nos formulários percorrem as páginas disponíveis, sem o antigo corte silencioso em 100/200 registros.

## Área do aluno e professor

O aluno usa endpoints pessoais `/api/v1/portal/*`, que resolvem organização e pessoa pelas claims. Parâmetros enviados pelo navegador não selecionam outro aluno. Os endpoints administrativos continuam sujeitos a papéis e permissões de gestão.

O check-in respeita a configuração da academia, o vínculo ativo à modalidade e o início da aula. A confirmação pode ser exigida ou automática; um check-in automático pode ser cancelado pelo próprio aluno antes do início. A validação por professor encerra essa possibilidade.

A agenda, as chamadas e os check-ins pendentes do professor são filtrados no servidor pelas aulas associadas a ele, inclusive nas rotas usadas pelo Admin.

## Organização do código

- `Services/AdminNavigation.cs`: regras de navegação e acesso visual.
- `Services/AdminApiClient*.cs`: comunicação HTTP e renovação da sessão.
- `Components/Common`: controle de rotas, paginação, identidade da academia e painel de evolução reutilizável.
- `Components/Pages/Workspaces`: entradas Gym/Fight.
- `Components/Pages/People`: pessoas e gestão de contas.
- `Components/Pages/Student`: componentes independentes de aulas e financeiro pessoal; metas e histórico reutilizam os componentes de `Common`.
- As telas novas separam apresentação `.razor` e lógica `.razor.cs`.

## Recuperação por e-mail

A recuperação pode entregar o link usando SMTP com TLS. Configure na API, fora do repositório:

- `Smtp__Host`, `Smtp__Port` (padrão 587).
- `Smtp__Username`, `Smtp__Password`, `Smtp__From`.
- `Smtp__ResetUrl`, por exemplo `https://painel.sua-academia.com/reset-password`.

A URL precisa usar HTTPS. O token segue no fragmento da URL, sem ser enviado na URL HTTP ao servidor do painel. A resposta não informa se o e-mail existe. Sem SMTP configurado, o responsável pode fornecer nova senha temporária em **Pessoas e acessos**. Nenhuma credencial SMTP ou entrega externa foi configurada nesta sessão.

## Validar

```bash
dotnet test backend/kiai-control-api/KiaiControl.Api.slnx
dotnet test frontend/kiai-control-admin/KiaiControl.Admin.slnx
```

Os testes PostgreSQL/E2E exigem Docker. Instale o Chromium do Playwright conforme `tests/README.md`. Os E2E criam bancos descartáveis, iniciam API/Admin reais e verificam gestão, permissões, portal pessoal, recuperação de senha e layouts em 390 px. As capturas de Gym/Fight são gravadas em `kiai-admin-preview` dentro do diretório temporário do sistema.

O workflow `.github/workflows/backend-api-ci.yml` executa build e as suítes. Mudanças apenas no banco também acionam a validação.
