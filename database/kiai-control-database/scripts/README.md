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

O seed não cria organização nem usuário administrador. O primeiro proprietário deve ser criado pelo fluxo seguro de cadastro da organização; não use credenciais padrão em produção.
