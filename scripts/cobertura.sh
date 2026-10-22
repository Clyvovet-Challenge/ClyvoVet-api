#!/usr/bin/env bash
# Cobertura de linhas das camadas de Domínio e Aplicação (rubric, item 4), unindo os testes de
# unidade e os de integração: o rubric pede cobertura das camadas, não de um tipo de teste.
# Sai com 1 abaixo do LIMITE — é uma catraca: a medição inicial (F5) foi 96,9%, e um limite
# bem abaixo disso deixaria a cobertura cair sem ninguém perceber.
#
# Uso:   scripts/cobertura.sh
#        DOTNET=~/.dotnet/dotnet scripts/cobertura.sh   # quando o dotnet do PATH não tem o runtime 8
set -euo pipefail

LIMITE="${LIMITE:-90}"
DOTNET="${DOTNET:-dotnet}"

cd "$(dirname "$0")/.."
SAIDA="TestResults/cobertura"
rm -rf "$SAIDA"

# Um .csproj por vez: o SDK 8 não lê o .slnx.
for projeto in tests/ClyvoVet.Api.Tests.Unit/ClyvoVet.Api.Tests.Unit.csproj \
               tests/ClyvoVet.Api.Tests.Integration/ClyvoVet.Api.Tests.Integration.csproj; do
  "$DOTNET" test "$projeto" --collect:"XPlat Code Coverage" --results-directory "$SAIDA/brutos"
done

"$DOTNET" tool restore > /dev/null
"$DOTNET" tool run reportgenerator \
  "-reports:$SAIDA/brutos/**/coverage.cobertura.xml" \
  "-targetdir:$SAIDA/relatorio" \
  "-reporttypes:Html;TextSummary" \
  "-assemblyfilters:+ClyvoVet.Domain;+ClyvoVet.Application" > /dev/null

COBERTURA=$(awk -F': *' '/^ *Line coverage:/ { sub("%", "", $2); print $2; exit }' "$SAIDA/relatorio/Summary.txt")
echo "Cobertura de linhas (Domain + Application): ${COBERTURA}% — limite ${LIMITE}%"
echo "Relatório: $SAIDA/relatorio/index.html"

if awk -v c="$COBERTURA" -v l="$LIMITE" 'BEGIN { exit !(c < l) }'; then
  echo "ABAIXO do limite." >&2
  exit 1
fi
