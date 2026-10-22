#!/usr/bin/env bash
# Exporta o documento OpenAPI da API para docs/swagger/openapi-v1.json (rubric, item 5:
# "Swagger exportado"). O arquivo é gerado pela própria API, não escrito à mão, então
# rodar de novo depois de mudar uma rota é o que o mantém fiel ao código.
#
# Sobe a API em Production numa porta local: esse ambiente não carrega user-secrets, então
# nenhuma credencial real é lida, e o Swagger não precisa de banco para responder.
#
# Uso:   scripts/exportar-swagger.sh
#        DOTNET=~/.dotnet/dotnet scripts/exportar-swagger.sh   # quando o dotnet do PATH não tem o runtime 8
set -euo pipefail

DOTNET="${DOTNET:-dotnet}"
PORTA="${PORTA:-5299}"

cd "$(dirname "$0")/.."
DESTINO="docs/swagger/openapi-v1.json"
URL="http://127.0.0.1:$PORTA/swagger/v1/swagger.json"
LOG="$(mktemp)"

"$DOTNET" build src/ClyvoVet.Api/ClyvoVet.Api.csproj --nologo -v quiet > /dev/null

ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="http://127.0.0.1:$PORTA" \
  "$DOTNET" run --project src/ClyvoVet.Api/ClyvoVet.Api.csproj --no-build --no-launch-profile > "$LOG" 2>&1 &
API_PID=$!
trap 'kill "$API_PID" 2> /dev/null || true; rm -f "$LOG"' EXIT

# Até 60 s para a API subir; se o processo morrer antes, mostra o log e desiste.
for _ in $(seq 1 60); do
  if curl -sf "$URL" -o /dev/null; then break; fi
  if ! kill -0 "$API_PID" 2> /dev/null; then
    echo "A API não subiu:" >&2; cat "$LOG" >&2; exit 1
  fi
  sleep 1
done

mkdir -p "$(dirname "$DESTINO")"
# Formatado (indentado) para o diff do Git mostrar só a rota que mudou.
curl -sf "$URL" | python3 -m json.tool --indent 2 > "$DESTINO"

ROTAS=$(python3 -c "import json,sys; print(len(json.load(open(sys.argv[1]))['paths']))" "$DESTINO")
echo "OpenAPI exportado em $DESTINO ($ROTAS rotas)."
