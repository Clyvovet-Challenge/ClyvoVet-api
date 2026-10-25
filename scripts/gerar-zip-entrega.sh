#!/usr/bin/env bash
# Monta o ZIP da entrega para o professor.
#
# O que entra: SÓ o que está no Git (git archive) no commit pedido, mais um AVALIACAO.md na raiz,
# montado a partir de docs/avaliacao-modelo.md com as credenciais preenchidas. Zipar a pasta do
# projeto à mão levaria junto o que existe apenas no disco (CLAUDE*.md, docs internos, bin/, obj/,
# Logs/); o git archive não leva, e a verificação final do script recusa um zip que os contenha.
#
# As credenciais NÃO vão para o Git: são lidas aqui (read -s, sem aparecer na tela) e gravadas só
# dentro do zip. Para testes automatizados, ENTREGA_API_KEY e ENTREGA_SENHA_DEMO substituem a digitação.
#
# Uso:  bash scripts/gerar-zip-entrega.sh [ref-git] [pasta-de-saída]
#       (padrão: main, na pasta atual; o arquivo se chama ClyvoVet-api-entrega.zip)
#
# O zip gerado TEM credenciais: não o suba para o Git nem para o GitHub (o .gitignore o ignora).

set -euo pipefail

REF="${1:-main}"
DESTINO="${2:-.}"
NOME_DA_PASTA="ClyvoVet-api"

cd "$(git rev-parse --show-toplevel)"

git rev-parse --verify --quiet "${REF}^{commit}" >/dev/null || {
  echo "ERRO: a ref '$REF' não existe neste repositório." >&2
  exit 1
}
COMMIT="$(git rev-parse --short "$REF")"

if [ -z "${ENTREGA_API_KEY:-}" ]; then
  read -r -s -p "X-Api-Key da API .NET no ar (não aparece): " ENTREGA_API_KEY
  echo
fi
if [ -z "${ENTREGA_SENHA_DEMO:-}" ]; then
  read -r -s -p "Senha das contas de demonstração da Java (não aparece): " ENTREGA_SENHA_DEMO
  echo
fi
if [ -z "$ENTREGA_API_KEY" ] || [ -z "$ENTREGA_SENHA_DEMO" ]; then
  echo "ERRO: a chave e a senha não podem ficar vazias." >&2
  exit 1
fi
export ENTREGA_API_KEY ENTREGA_SENHA_DEMO
export ENTREGA_COMMIT="$COMMIT" ENTREGA_DATA="$(date +%d/%m/%Y)"

mkdir -p "$DESTINO"
SAIDA="$(cd "$DESTINO" && pwd)/ClyvoVet-api-entrega.zip"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

git archive --format=zip --prefix="$NOME_DA_PASTA/" -o "$TMP/base.zip" "$REF"

# Os valores vão por variável de ambiente (nunca por argumento de comando).
python3 - "$TMP/base.zip" "$SAIDA" "$NOME_DA_PASTA" <<'PY'
import os
import shutil
import sys
import zipfile

base, saida, pasta = sys.argv[1], sys.argv[2], sys.argv[3]

with zipfile.ZipFile(base) as z:
    modelo = z.read(f"{pasta}/docs/avaliacao-modelo.md").decode("utf-8")

valores = {
    "{{API_KEY}}": os.environ["ENTREGA_API_KEY"],
    "{{SENHA_DEMO}}": os.environ["ENTREGA_SENHA_DEMO"],
    "{{COMMIT}}": os.environ["ENTREGA_COMMIT"],
    "{{DATA}}": os.environ["ENTREGA_DATA"],
}
for marcador, valor in valores.items():
    modelo = modelo.replace(marcador, valor)

if "{{" in modelo:
    sys.exit("ERRO: sobrou um marcador sem valor em docs/avaliacao-modelo.md")

shutil.copyfile(base, saida)
with zipfile.ZipFile(saida, "a", zipfile.ZIP_DEFLATED) as z:
    z.writestr(f"{pasta}/AVALIACAO.md", modelo)
PY

# Verificação final: nada que só existe no disco, ou que é interno, pode estar no zip.
PROIBIDOS='(^|/)(CLAUDE[^/]*\.md|\.git/|\.claude/|bin/|obj/|TestResults/|Logs/|docs/sprint4/|documentos/)'
if unzip -Z1 "$SAIDA" | grep -E "$PROIBIDOS" >/dev/null; then
  echo "ERRO: o zip contém arquivos que não deveriam estar nele:" >&2
  unzip -Z1 "$SAIDA" | grep -E "$PROIBIDOS" >&2
  rm -f "$SAIDA"
  exit 1
fi

ARQUIVOS="$(unzip -Z1 "$SAIDA" | grep -vc '/$' || true)"
TAMANHO="$(du -h "$SAIDA" | cut -f1)"
echo
echo "Zip gerado: $SAIDA"
echo "  commit:   $COMMIT ($REF)"
echo "  arquivos: $ARQUIVOS ($TAMANHO)"
echo "  AVALIACAO.md na raiz, com as credenciais preenchidas."
echo
echo "ATENÇÃO: este zip tem credenciais. Envie só ao professor; não o suba para o Git nem para o GitHub."
