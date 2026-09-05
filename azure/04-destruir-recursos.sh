#!/usr/bin/env bash
# apaga tudo que foi criado nessa entrega, pra parar de gastar.
set -euo pipefail
cd "$(dirname "$0")"
source ./00-variaveis.sh

az account set --subscription "$SUBSCRIPTION"

read -r -p "Isso vai apagar o Resource Group '$RG' e TODOS os recursos dentro dele. Confirma? (digite 'sim'): " CONFIRM
if [ "$CONFIRM" != "sim" ]; then
    echo "Cancelado."
    exit 0
fi

echo "==> Apagando Resource Group $RG (assíncrono)..."
az group delete --name "$RG" --yes --no-wait

echo "==> Solicitação de exclusão enviada. Acompanhe com:"
echo "    az group show --name $RG"
