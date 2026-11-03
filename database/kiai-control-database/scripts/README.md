# Preparação do banco de produção

Os scripts abaixo criam o schema atual do Kiai Control e inserem os dados globais de autorização:

1. `0001_create_production_schema.sql`
2. `0002_seed_production_reference_data.sql`

Execute na ordem acima, conectado ao banco que será usado pela aplicação:

```bash
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f 0001_create_production_schema.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f 0002_seed_production_reference_data.sql
```

Ambos podem ser executados novamente. O schema usa `IF NOT EXISTS`; o seed usa chaves únicas e `ON CONFLICT` para evitar duplicidades.

O bootstrap representa o schema final atual e é destinado a um banco novo. Ele não substitui as migrations para atualizar um banco existente: `CREATE TABLE IF NOT EXISTS` não altera tabelas já criadas nem reconcilia diferenças de schema. Para atualizações, use o fluxo de migrations do projeto.

Requer PostgreSQL 15 ou superior. A criação da extensão `pgcrypto` e do schema também requer permissões adequadas para o usuário que executa os scripts.

O seed não cria organização nem usuários. Para provisionar o primeiro dono global da plataforma, configure no ambiente da API:

- `PlatformOwner__Email`
- `PlatformOwner__FullName`
- `PlatformOwner__Password`

Configure as três variáveis juntas. A senha deve ter pelo menos 12 caracteres e não deve ser armazenada no repositório, em arquivos versionados ou em logs. No primeiro início da API, o bootstrap cria a conta de forma idempotente; a senha inicial será temporária e deverá ser trocada no primeiro acesso. Depois, o dono global cadastra as organizações pelo painel, e cada organização administra as contas de acesso da própria equipe.

Para bancos existentes, aplique a migration `010_platform_owner_and_temporary_passwords.sql` antes de iniciar a nova versão da API. Usando `DATABASE_URL`:

```bash
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 \
  -f ../../migrations/010_platform_owner_and_temporary_passwords.sql
```

Execute o comando a partir de `database/kiai-control-database/scripts/start_production`, ou ajuste o caminho do arquivo. A migration e segura para reexecucao (`ADD COLUMN IF NOT EXISTS`). Depois de aplicada, confirme que ambas as colunas existem antes de reiniciar a API:

```sql
select column_name
from information_schema.columns
where table_schema = 'public'
  and table_name = 'user_accounts'
  and column_name in ('is_platform_owner', 'must_change_password')
order by column_name;
```
