# schema/ — scripts do banco (MySQL)

O banco é **MySQL** e é compartilhado com a API Java, que é a dona do schema (Flyway). O passo a passo
completo está no [README principal](../README.md#passo-3--preparar-o-banco-de-dados).

| Arquivo | Para quê |
|---|---|
| [`script_bd.sql`](script_bd.sql) | **Desenvolvimento local.** Cria o schema inteiro das duas APIs, com seed, num MySQL vazio. Não use no banco da nuvem |
| [`ef/baseline-banco-compartilhado.sql`](ef/baseline-banco-compartilhado.sql) | **Banco compartilhado.** Marca a migration `Inicial` do EF como aplicada num banco que o Flyway da Java já montou |
| [`ef/migrations-idempotente.sql`](ef/migrations-idempotente.sql) | **Banco compartilhado.** Aplica as migrations do EF que faltarem; rodar duas vezes não faz nada na segunda. É gerado por `scripts/gerar-script-migrations.sh` |
| [`legado-oracle/`](legado-oracle/) | Registro histórico dos scripts do tempo do Oracle. **Não rodam no MySQL** |

**Num banco compartilhado, a ordem é sempre:** Flyway da API Java → `ef/baseline-banco-compartilhado.sql`
→ `ef/migrations-idempotente.sql`. Rodar o script do EF antes do Flyway, num banco vazio, impede a API
Java de subir. O porquê está na seção
[3.3 — Migrations do EF Core](../README.md#33--migrations-do-ef-core) do README.
