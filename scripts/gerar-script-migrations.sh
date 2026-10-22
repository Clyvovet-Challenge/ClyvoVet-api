#!/usr/bin/env bash
# Gera schema/ef/migrations-idempotente.sql a partir das migrations do EF (rubric, item 3:
# "EF Core com migrações aplicadas"). O arquivo é gerado, não escrito à mão: rodar de novo
# depois de mudar uma entidade ou configuração (e criar a migration) é o que o mantém fiel.
#
# Idempotente: cada migration só roda se não estiver em __EFMigrationsHistory, então aplicar
# o arquivo duas vezes no mesmo banco não faz nada na segunda.
#
# Não conecta em banco nenhum: quem monta o contexto é a AppDbContextFactory.
#
# Uso:   scripts/gerar-script-migrations.sh
#        DOTNET=~/.dotnet/dotnet scripts/gerar-script-migrations.sh   # quando o dotnet do PATH não tem o runtime 8
set -euo pipefail

DOTNET="${DOTNET:-dotnet}"

cd "$(dirname "$0")/.."
DESTINO="schema/ef/migrations-idempotente.sql"
TEMP="$(mktemp)"
trap 'rm -f "$TEMP"' EXIT

"$DOTNET" tool restore > /dev/null
"$DOTNET" ef migrations script --idempotent \
  --project src/ClyvoVet.Infrastructure \
  --startup-project src/ClyvoVet.Api \
  --output "$TEMP" > /dev/null

mkdir -p "$(dirname "$DESTINO")"
# Sem o BOM que o EF grava no começo do arquivo: há cliente mysql que o lê como parte
# do primeiro comando e acusa erro de sintaxe no CREATE TABLE.
sed '1s/^\xEF\xBB\xBF//' "$TEMP" > "$DESTINO"

MIGRATIONS=$(grep -c "INSERT INTO \`__EFMigrationsHistory\`" "$DESTINO")
echo "Script idempotente gerado em $DESTINO ($MIGRATIONS migration(s))."
