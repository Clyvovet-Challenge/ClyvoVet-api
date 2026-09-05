#!/usr/bin/env bash
# variaveis usadas pelos scripts 01-04. ajusta aqui antes de rodar
# (nome de servidor/app tem que ser unico no Azure inteiro)

export SUBSCRIPTION="2TDSPW-RM563065-LEONARDO-PEREIRA"
export RG="rg-clyvovet-devops"
export RG_LOCATION="brazilsouth"       # o RG em si já existia nessa região
# a assinatura da FIAP só libera algumas regiões (southcentralus, mexicocentral,
# chilecentral, canadacentral, eastus2) e mesmo assim testei brazilsouth/eastus2/
# southcentralus pro Postgres Flexible Server e deu erro nas 3 - só canadacentral criou
export LOCATION="canadacentral"

export PSQL_SERVER="psql-clyvovet-rm563065"        # vira <servidor>.postgres.database.azure.com
export PSQL_DB="clyvovet"
export PSQL_ADMIN="clyvovetadmin"
# nunca bota a senha real aqui, exporta antes de rodar:
# export PSQL_PASSWORD='SuaSenhaForte123!'
if [ -z "$PSQL_PASSWORD" ]; then
    echo "[ERRO] defina PSQL_PASSWORD no ambiente antes de rodar (ex: export PSQL_PASSWORD='SuaSenhaForte123!')" >&2
    return 1 2>/dev/null || exit 1
fi

export PLAN_NAME="plan-clyvovet-devops"
export APP_NAME="app-clyvovet-devops-rm563065"     # vira <app>.azurewebsites.net
