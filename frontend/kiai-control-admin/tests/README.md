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
