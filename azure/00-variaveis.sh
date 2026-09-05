#!/usr/bin/env bash
# variaveis usadas pelos scripts 01-04. ajusta aqui antes de rodar


export SUBSCRIPTION="2TDSPW-RM563065-LEONARDO-PEREIRA"
export RG="rg-clyvovet-devops"
export RG_LOCATION="brazilsouth"       # o RG em si já existia nessa região
export LOCATION="mexicocentral"

export MYSQL_SERVER="mysql-clyvovet-rm563065"      # vira <servidor>.mysql.database.azure.com
export MYSQL_DB="clyvovet"
export MYSQL_ADMIN="clyvovetadmin"
if [ -z "$MYSQL_PASSWORD" ]; then
    echo "[ERRO] defina MYSQL_PASSWORD no ambiente antes de rodar (ex: export MYSQL_PASSWORD='SuaSenhaForte123!')" >&2
    return 1 2>/dev/null || exit 1
fi

export PLAN_NAME="plan-clyvovet-devops"
export APP_NAME="app-clyvovet-devops-rm563065"     # vira <app>.azurewebsites.net
