# Testes do Kiai Control Admin

O projeto `KiaiControl.Admin.UnitTests` contem testes de unidade do cliente HTTP e testes bUnit dos componentes compartilhados.

Execute a suite a partir da raiz do repositorio:

```bash
dotnet test frontend/kiai-control-admin/KiaiControl.Admin.slnx
```

O projeto `KiaiControl.Admin.E2ETests` executa no Chromium fluxos de login, cadastro/edicao e jornada consolidada do aluno, cadastro do professor, associacao a horario recorrente e consulta de agenda, vinculo de modalidade, geracao de aula, check-in, assinatura e pagamento. Tambem valida o bootstrap do banco vazio executando duas vezes o script `create_database.sh`, conferindo migrations, seed e login do administrador. Os testes iniciam API e Admin locais e usam PostgreSQL 17 via Testcontainers; Docker precisa estar em execucao.

Para executar apenas o E2E, restaure/construa o projeto, instale o browser Playwright e rode o teste:

```bash
dotnet build frontend/kiai-control-admin/tests/KiaiControl.Admin.E2ETests/KiaiControl.Admin.E2ETests.csproj
pwsh frontend/kiai-control-admin/tests/KiaiControl.Admin.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test frontend/kiai-control-admin/tests/KiaiControl.Admin.E2ETests/KiaiControl.Admin.E2ETests.csproj
```

O plano de evolucao e os criterios de aceite estao em [PROJECT_IMPLEMENTATION_PLAN.md](../../../PROJECT_IMPLEMENTATION_PLAN.md).

## Cobertura da conclusão do painel

A suíte contém 34 testes de unidade/componentes e 13 cenários E2E. Os cenários adicionais validam diretório/equipe, reaproveitamento de pessoas pelos formulários, bloqueio de conta, consulta sem gravação, módulos Gym/Fight e responsividade, portal pessoal com senha temporária e check-in, privacidade financeira/metas, escopo do professor, perfil financeiro e recuperação de senha com revogação de sessões. A página de aulas também é validada com API, Admin e navegador em `America/Sao_Paulo`, cobrindo filtros com offset `-03:00` e exibição do horário local.

Três jornadas verificam as visões Geral/Fight/Gym para owner, admin e recepção: filtros na API, paginação e totais, vínculos duplos/híbridos, alunos sem modalidade, seleção pela área, troca de visão sem sair da tela, persistência após reload e opções de metas/graduação. A recepção não ganha acesso a módulos fora de suas permissões. Capturas `students-gym-mobile.png` e `students-general-desktop.png` ficam em `kiai-admin-preview` no diretório temporário do sistema.

As jornadas usam senhas válidas de seis caracteres no bootstrap, cadastro de academia, criação de acesso, primeiro acesso, troca e recuperação. Também verificam a mensagem de validação antes de aceitar senhas incompletas e a troca em Minha conta.

Os testes de navegador usam credenciais e dados exclusivamente descartáveis; a recuperação recebe um token de fixture, sem enviar e-mail real. O backend soma 81 testes unitários, 23 de integração e dois arquiteturais; o app possui 26 testes, sendo nove de modelos e 17 da política de senha. A integração inclui buscas parametrizadas com caracteres especiais e tentativas de SQL injection; a arquitetura verifica que as classes de queries expõem constantes SQL. As capturas Gym/Fight ficam no diretório temporário `kiai-admin-preview`. A evidência mais recente está em `PROJECT_IMPLEMENTATION_PLAN.md`.
