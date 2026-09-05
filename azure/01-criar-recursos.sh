#!/usr/bin/env bash
# cria o resource group + o Postgres Flexible Server na Azure via CLI
set -euo pipefail
cd "$(dirname "$0")"
source ./00-variaveis.sh

az account set --subscription "$SUBSCRIPTION"

echo "==> Criando Resource Group $RG em $RG_LOCATION..."
az group create \
    --name "$RG" \
    --location "$RG_LOCATION"

echo "==> SKUs Burstable disponíveis em $LOCATION (conferir antes de criar):"
az postgres flexible-server list-skus --location "$LOCATION" -o table || true

echo "==> Criando Azure Database for PostgreSQL Flexible Server ($PSQL_SERVER)..."
# --database-name só funciona com --node-count (cluster elastic), então cria com o
# banco padrão "postgres" e cria o banco de verdade no passo seguinte
az postgres flexible-server create \
    --resource-group "$RG" \
    --name "$PSQL_SERVER" \
    --location "$LOCATION" \
    --admin-user "$PSQL_ADMIN" \
    --admin-password "$PSQL_PASSWORD" \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --storage-size 32 \
    --version 16 \
    --public-access 0.0.0.0 \
    --yes

echo "==> Criando o banco de dados ($PSQL_DB) dentro do servidor..."
az postgres flexible-server db create \
    --resource-group "$RG" \
    --server-name "$PSQL_SERVER" \
    --name "$PSQL_DB"

echo "==> Liberando acesso do seu IP atual no firewall (para rodar o script_bd.sql via psql)..."
# tem que ser IPv4, o firewall do Postgres não aceita IPv6 e minha rede usa IPv6 por padrão
MY_IP="$(curl -4 -s https://api.ipify.org)"
az postgres flexible-server firewall-rule create \
    --resource-group "$RG" \
    --server-name "$PSQL_SERVER" \
    --name AllowMyIP \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP"

echo "==> Recursos de banco criados. Host: $PSQL_SERVER.postgres.database.azure.com"
echo "==> Próximo passo: aplicar o schema/script_bd.sql contra esse servidor, ex:"
echo "    psql \"host=$PSQL_SERVER.postgres.database.azure.com port=5432 dbname=$PSQL_DB user=$PSQL_ADMIN password=$PSQL_PASSWORD sslmode=require\" -f ../schema/script_bd.sql"
